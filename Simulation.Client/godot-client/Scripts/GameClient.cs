using System.Collections.Generic;
using System.Linq;
using Game.Network.Abstractions;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using GodotClient.Player;
using Godot;

namespace GodotClient;

public partial class GameClient : Node
{
    private API.ApiClient? _apiClient;
    private ConfigManager? _configManager;
    private PlayerRoot? _playerView;
    private GodotInputSystem? _inputSystem;
    private INetworkManager? _network;
    
    private LoginConfiguration _login = new();
    private RegistrationConfiguration _registration = new();
    private CharacterCreationConfiguration _characterCreationConfiguration = new();
    private CharacterSelectionConfiguration _characterSelectionConfiguration = new();
    
    private Label? _statusLabel;
    private bool _registrationAttempted;
    private bool _loginAttempted;

    private readonly Dictionary<int, PlayerSnapshot> _players = new();
    private List<PlayerCharData> _availableCharacters = [];
    private bool _isAuthenticated;
    private int _localNetworkId = -1;

    public bool CanSendInput => _isAuthenticated && _network is not null && _localNetworkId != -1;

    public override void _Ready()
    {
        base._Ready();

        _apiClient = GetNode<API.ApiClient>($"%{nameof(API.ApiClient)}");
        _configManager = GetNode<ConfigManager>($"%{nameof(ConfigManager)}");
        _playerView = GetNode<PlayerRoot>(nameof(PlayerRoot));
        _inputSystem = GetNode<GodotInputSystem>(nameof(GodotInputSystem));

        _inputSystem.Attach(this);

        var statusLayer = new CanvasLayer { Name = "HudLayer" };
        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Position = new Vector2(12, 12),
            Text = "Inicializando..."
        };
        _statusLabel.AddThemeColorOverride("font_color", Colors.White);
        statusLayer.AddChild(_statusLabel);
        AddChild(statusLayer);

        var networkOptions = _configManager.CreateNetworkOptions();
        _login = _configManager.GetLoginConfiguration();
        _registration = _configManager.GetRegistrationConfiguration();

        _network = _apiClient.Initialize(networkOptions);
        _network.OnPeerConnected += OnPeerConnected;
        _network.OnPeerDisconnected += OnPeerDisconnected;

        _network.RegisterPacketHandler<LoginResponsePacket>(HandleLoginResponse);
        _network.RegisterPacketHandler<RegistrationResponsePacket>(HandleRegistrationResponse);
        _network.RegisterPacketHandler<CharacterCreationResponsePacket>(HandleCharacterCreationResponse);
        _network.RegisterPacketHandler<CharacterSelectionResponsePacket>(HandleCharacterSelectionResponse);
        _network.RegisterPacketHandler<GameDataPacket>(HandleGameData);
        _network.RegisterPacketHandler<PlayerSpawnPacket>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<PlayerMovementPacket>(HandlePlayerMovement);
        _network.RegisterPacketHandler<PlayerDespawnPacket>(HandlePlayerDespawn);

        _apiClient.Start();

        UpdateStatus($"Connecting to server {networkOptions.ServerAddress}:{networkOptions.ServerPort}...");
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (_network is not null)
        {
            _network.OnPeerConnected -= OnPeerConnected;
            _network.OnPeerDisconnected -= OnPeerDisconnected;
            _network.UnregisterPacketHandler<LoginResponsePacket>();
            _network.UnregisterPacketHandler<RegistrationResponsePacket>();
            _network.UnregisterPacketHandler<CharacterCreationResponsePacket>();
            _network.UnregisterPacketHandler<CharacterSelectionResponsePacket>();
            _network.UnregisterPacketHandler<GameDataPacket>();
            _network.UnregisterPacketHandler<PlayerSpawnPacket>();
            _network.UnregisterPacketHandler<PlayerMovementPacket>();
            _network.UnregisterPacketHandler<PlayerDespawnPacket>();
        }

        _inputSystem?.Detach();
    }

    public void QueueInput(sbyte moveX, sbyte moveY, ushort buttons)
    {
        if (!CanSendInput || _network is null)
            return;

        var packet = new PlayerInputPacket(moveX, moveY, buttons);
        
        GD.Print($"Sending input: MoveX={moveX}, MoveY={moveY}, Buttons={buttons}");
        
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.Sequenced);
    }

    private void OnPeerConnected(INetPeerAdapter peer)
    {
        UpdateStatus("Connected to server. Preparing session...");

        if (_registration.AutoRegister && !_registrationAttempted)
        {
            TrySendRegistration();
            return;
        }

        if (_login.AutoLogin && !_loginAttempted)
        {
            TrySendLogin();
        }
    }

    private void OnPeerDisconnected(INetPeerAdapter peer)
    {
        GD.PushWarning("Disconnected from server.");
        ResetState();
        UpdateStatus("Disconnected from server");
    }

    private void TrySendRegistration()
    {
        if (_network is null || _registrationAttempted)
            return;

        _registrationAttempted = true;

        // Use credenciais de Registration se disponíveis, senão fallback para Login
        var username = !string.IsNullOrWhiteSpace(_registration.Username) 
            ? _registration.Username 
            : _login.Username;
        var password = !string.IsNullOrWhiteSpace(_registration.Password) 
            ? _registration.Password 
            : _login.Password;
        var email = _registration.Email;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            UpdateStatus("Registro não enviado: informe usuário e senha em appsettings.json.");
            _registrationAttempted = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            UpdateStatus("Registro não enviado: informe o e-mail em appsettings.json.");
            _registrationAttempted = false;
            return;
        }

        var packet = new RegistrationRequestPacket(username, email, password);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus($"Enviando registro para usuário '{username}'...");
    }

    private void TrySendLogin()
    {
        if (_network is null || _loginAttempted)
            return;

        if (string.IsNullOrWhiteSpace(_login.Username) || string.IsNullOrWhiteSpace(_login.Password))
        {
            GD.PushWarning("Login configuration is missing username or password. Update appsettings.json.");
            UpdateStatus("Login configuration is incomplete.");
            return;
        }

        var packet = new LoginRequestPacket(_login.Username, _login.Password);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus($"Authenticating as '{_login.Username}'...");
        _loginAttempted = true;
    }
    
    private void TrySendCharacterCreation()
    {
        if (_network is null || !_isAuthenticated)
            return;

        if (string.IsNullOrWhiteSpace(_characterCreationConfiguration.Name))
        {
            GD.PushWarning("Character creation requires a valid name.");
            UpdateStatus("Character creation failed: no name specified.");
            return;
        }

        var packet = new CharacterCreationRequestPacket(
            _characterCreationConfiguration.Name,
            _characterCreationConfiguration.Gender,
            _characterCreationConfiguration.Vocation);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus($"Creating character '{_characterCreationConfiguration.Name}'...");
    }
    
    private void TrySendCharacterSelection()
    {
        if (_network is null || !_isAuthenticated)
            return;

        if (_characterSelectionConfiguration.CharacterId <= 0)
        {
            GD.PushError("Invalid character ID for selection.");
            UpdateStatus("Character selection failed: invalid ID.");
            return;
        }

        var packet = new CharacterSelectionRequestPacket(_characterSelectionConfiguration.CharacterId);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus($"Selecting character ID '{_characterSelectionConfiguration.CharacterId}'...");
    }

    private void HandleLoginResponse(INetPeerAdapter peer, LoginResponsePacket packet)
    {
        if (!packet.Success)
        {
            GD.PushError($"Login failed: {packet.Message}");
            UpdateStatus($"Login failed: {packet.Message}");
            _loginAttempted = false;
            return;
        }
        
        UpdateStatus($"Logged in successfully. Characters available: {packet.CurrentCharacters.Length}");

        _isAuthenticated = true;
        _availableCharacters.Clear();
        _availableCharacters.AddRange(packet.CurrentCharacters);
        
        if (_availableCharacters.Count == 0)
        {
            _characterCreationConfiguration = _configManager?.GetCharacterCreationConfiguration() 
                ?? new CharacterCreationConfiguration();
            TrySendCharacterCreation();
        }
        else
        {
            _characterSelectionConfiguration = _configManager?.GetCharacterSelectionConfiguration() 
                ?? new CharacterSelectionConfiguration();
            
            // Valida se o CharacterId existe na lista
            if (_characterSelectionConfiguration.CharacterId > 0)
            {
                var exists = _availableCharacters.Any(c => c.Id == _characterSelectionConfiguration.CharacterId);
                if (!exists)
                {
                    GD.PushWarning($"Configured CharacterId {_characterSelectionConfiguration.CharacterId} not found. Using first available character.");
                    _characterSelectionConfiguration.CharacterId = _availableCharacters[0].Id;
                }
            }
            else
            {
                // Se não configurado, usa o primeiro
                _characterSelectionConfiguration.CharacterId = _availableCharacters[0].Id;
            }
            
            TrySendCharacterSelection();
        }
    }

    private void HandleRegistrationResponse(INetPeerAdapter peer, RegistrationResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus("Account created successfully! Attempting login...");
            
            // Tenta login automaticamente após registro bem-sucedido
            if (_login.AutoLogin && !_loginAttempted)
            {
                TrySendLogin();
            }
            return;
        }

        GD.PushError($"Registration failed: {packet.Message}");
        UpdateStatus($"Registration failed: {packet.Message}");

        // Se o registro falhar (ex: conta já existe), tenta login
        if (_login.AutoLogin && !_loginAttempted)
        {
            UpdateStatus("Registration failed, attempting login with existing account...");
            TrySendLogin();
        }
    }
    
    private void HandleCharacterCreationResponse(INetPeerAdapter peer, CharacterCreationResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus($"Character '{packet.CreatedCharacter.Name}' created successfully!");
            _availableCharacters.Add(packet.CreatedCharacter);
            
            _characterSelectionConfiguration = _configManager?.GetCharacterSelectionConfiguration() 
                ?? new CharacterSelectionConfiguration();
            _characterSelectionConfiguration.CharacterId = packet.CreatedCharacter.Id;
            TrySendCharacterSelection();
            
            return;
        }

        GD.PushError($"Character creation failed: {packet.Message}");
        UpdateStatus($"Character creation failed: {packet.Message}");
    }
    
    private void HandleCharacterSelectionResponse(INetPeerAdapter peer, CharacterSelectionResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus($"Character selected! Waiting for game data...");
            return;
        }

        GD.PushError($"Character selection failed: {packet.Message}");
        UpdateStatus($"Character selection failed: {packet.Message}");
    }
    
    private void HandleGameData(INetPeerAdapter peer, GameDataPacket packet)
    {
        UpdateStatus("Entering game world...");
        
        _localNetworkId = packet.LocalPlayer.NetworkId;
        _players[_localNetworkId] = packet.LocalPlayer;

        _playerView?.SetLocalPlayer(packet.LocalPlayer);

        foreach (var snapshot in packet.OtherPlayers)
        {
            if (_players.TryAdd(snapshot.NetworkId, snapshot))
            {
                _playerView?.ApplySnapshot(snapshot, false);
            }
        }
        
        UpdateStatus($"In game as '{packet.LocalPlayer.Name}' (NetID: {_localNetworkId})");
    }

    private void HandlePlayerSpawn(INetPeerAdapter peer, PlayerSpawnPacket packet)
    {
        if (_players.TryAdd(packet.Player.NetworkId, packet.Player))
        {
            var isLocal = packet.Player.NetworkId == _localNetworkId;
            _playerView?.ApplySnapshot(packet.Player, isLocal);
            
            GD.Print($"Player spawned: {packet.Player.Name} (NetID: {packet.Player.NetworkId})");
        }
    }

    private void HandlePlayerMovement(INetPeerAdapter peer, PlayerMovementPacket packet)
    {
        GD.Print($"Received movement for NetID {packet.NetworkId}: Pos({packet.Position.X},{packet.Position.Y}) Facing({packet.Facing})");
        
        if (_players.TryGetValue(packet.NetworkId, out var snapshot))
        {
            snapshot = new PlayerSnapshot(
                packet.NetworkId, 
                snapshot.PlayerId, 
                snapshot.CharacterId, 
                snapshot.Name, 
                snapshot.Gender, 
                snapshot.Vocation, 
                packet.Position, 
                packet.Facing);
            
            _players[packet.NetworkId] = snapshot;
            _playerView?.UpdateMovement(packet);
        }
    }

    private void HandlePlayerDespawn(INetPeerAdapter peer, PlayerDespawnPacket packet)
    {
        if (_players.Remove(packet.NetworkId))
        {
            _playerView?.RemovePlayer(packet.NetworkId);
            GD.Print($"Player despawned (NetID: {packet.NetworkId})");
        }
    }

    private void ResetState()
    {
        _isAuthenticated = false;
        _localNetworkId = -1;
        _registrationAttempted = false;
        _loginAttempted = false;
        _players.Clear();
        _availableCharacters.Clear();
        _playerView?.Clear();
    }

    private void UpdateStatus(string message)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = message;
        }
        GD.Print($"[GameClient] {message}");
    }
}