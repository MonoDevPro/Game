using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Game.Network.Abstractions;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using Godot;
using GodotClient.Systems;

namespace GodotClient.Scenes.Menu;

/// <summary>
/// Controlador do menu com autenticação completa (Login/Registro/Criação/Seleção).
/// Autor: MonoDevPro
/// Data: 2025-01-11 22:01:06
/// </summary>
public partial class MenuScript : Control
{
    private INetworkManager? _network;
    private Label? _statusLabel;
    
    // Configurações
    private NetworkOptions _netOptions = null!;
    private LoginConfiguration _login = null!;
    private RegistrationConfiguration _registration = null!;
    private CharacterCreationConfiguration _characterCreation = null!;
    private CharacterSelectionConfiguration _characterSelection = null!;
    
    // Estado de autenticação
    private bool _isAuthenticated;
    private bool _registrationAttempted;
    private bool _loginAttempted;
    private bool _gameDataReceived;
    
    // ✅ Cache de personagens disponíveis
    private readonly List<PlayerCharData> _availableCharacters = new();

    public override void _Ready()
    {
        base._Ready();

        CreateStatusLabel();
        LoadConfigurations();
        InitializeNetwork();
        
        GetNode<Window>("%LoginWindow").Visible = !_login.AutoLogin;
        GetNode<Button>("%LoginButton").Pressed += TrySendLogin;
        
        GetNode<Window>("%RegisterWindow").Visible = _registration.AutoRegister && !_login.AutoLogin;
        GetNode<Button>("%RegisterButton").Pressed += TrySendRegistration;
    }

    /// <summary>
    /// Cria label de status (HUD temporário).
    /// </summary>
    private void CreateStatusLabel()
    {
        var statusLayer = new CanvasLayer { Name = "HudLayer" };
        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Position = new Vector2(12, 12),
            Text = "Initializing..."
        };
        _statusLabel.AddThemeColorOverride("font_color", Colors.White);
        statusLayer.AddChild(_statusLabel);
        AddChild(statusLayer);
    }

    /// <summary>
    /// Carrega configurações do ConfigManager.
    /// </summary>
    private void LoadConfigurations()
    {
        var configManager = ConfigManager.Instance;
        _netOptions = configManager.CreateNetworkOptions();
        _login = configManager.GetLoginConfiguration();
        _registration = configManager.GetRegistrationConfiguration();
        _characterCreation = configManager.GetCharacterCreationConfiguration();
        _characterSelection = configManager.GetCharacterSelectionConfiguration();
        
        GD.Print("[Menu] Configurations loaded");
    }

    /// <summary>
    /// Inicializa rede e registra handlers.
    /// </summary>
    private void InitializeNetwork()
    {
        UpdateStatus($"Network Options: {_netOptions}");
        
        // ✅ Usa singleton NetworkClient
        _network = NetworkClient.Instance.Initialize(_netOptions);
        _network.OnPeerConnected += OnPeerConnected;
        _network.OnPeerDisconnected += OnPeerDisconnected;

        // ✅ Registra handlers de autenticação
        _network.RegisterPacketHandler<LoginResponsePacket>(HandleLoginResponse);
        _network.RegisterPacketHandler<RegistrationResponsePacket>(HandleRegistrationResponse);
        _network.RegisterPacketHandler<CharacterCreationResponsePacket>(HandleCharacterCreationResponse);
        _network.RegisterPacketHandler<CharacterSelectionResponsePacket>(HandleCharacterSelectionResponse);
        _network.RegisterPacketHandler<GameDataPacket>(ProcessGameData); 
        
        UpdateStatus("Network initialized.");
        
        // Conecta ao servidor
        UpdateStatus("Connecting to server...");
        NetworkClient.Instance.Start();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (_network is not null)
        {
            _network.OnPeerConnected -= OnPeerConnected;
            _network.OnPeerDisconnected -= OnPeerDisconnected;
            
            // ✅ Desregistra handlers
            _network.UnregisterPacketHandler<LoginResponsePacket>();
            _network.UnregisterPacketHandler<RegistrationResponsePacket>();
            _network.UnregisterPacketHandler<CharacterCreationResponsePacket>();
            _network.UnregisterPacketHandler<CharacterSelectionResponsePacket>();
            _network.UnregisterPacketHandler<GameDataPacket>();
        }
        
        GD.Print("[Menu] Unloaded");
    }

    // ========== EVENTOS DE REDE ==========

    private void OnPeerConnected(INetPeerAdapter peer)
    {
        UpdateStatus("Connected to server. Preparing session...");

        // Fluxo: Registro (se necessário) → Login
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
        GD.PushWarning("[Menu] Disconnected from server");
        ResetState();
        UpdateStatus("Disconnected from server");
    }

    // ========== ENVIO DE PACOTES ==========

    private void TrySendRegistration()
    {
        if (_network is null || _registrationAttempted)
            return;
        
        _registration.Username = GetNode<LineEdit>("%RegisterUserLineEdit").Text.Trim();
        _registration.Password = GetNode<LineEdit>("%RegisterPassLineEdit").Text.Trim();
        _registration.Email = GetNode<LineEdit>("%RegisterEmailLineEdit").Text.Trim();
        
        var registerConfirm = GetNode<LineEdit>("%RegisterPassConfirmLineEdit").Text.Trim();
        if (_registration.Password != registerConfirm)
        {
            UpdateStatus("Registration failed: passwords do not match");
            _registrationAttempted = false;
            return;
        }
        
        _registrationAttempted = true;

        var username = !string.IsNullOrWhiteSpace(_registration.Username) 
            ? _registration.Username 
            : _login.Username;
        var password = !string.IsNullOrWhiteSpace(_registration.Password) 
            ? _registration.Password 
            : _login.Password;
        var email = _registration.Email;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            UpdateStatus("Registration failed: username or password missing in config");
            _registrationAttempted = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            UpdateStatus("Registration failed: email missing in config");
            _registrationAttempted = false;
            return;
        }

        var packet = new RegistrationRequestPacket(username, email, password);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus($"Registering user '{username}'...");
        
        GetNode<Window>("%RegisterWindow").Visible = false;
        GetNode<Window>("%LoginWindow").Visible = true;
    }

    private void TrySendLogin()
    {
        if (_network is null || _loginAttempted)
            return;
        
        _login.Username = GetNode<LineEdit>("%LoginUserLineEdit").Text.Trim();
        _login.Password = GetNode<LineEdit>("%LoginPassLineEdit").Text.Trim();

        if (string.IsNullOrWhiteSpace(_login.Username) || string.IsNullOrWhiteSpace(_login.Password))
        {
            UpdateStatus("Login failed: credentials missing in config");
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

        if (string.IsNullOrWhiteSpace(_characterCreation.Name))
        {
            UpdateStatus("Character creation failed: no name specified");
            return;
        }

        var packet = new CharacterCreationRequestPacket(
            _characterCreation.Name,
            _characterCreation.Gender,
            _characterCreation.Vocation);
        
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus($"Creating character '{_characterCreation.Name}'...");
    }

    private void TrySendCharacterSelection()
    {
        if (_network is null || !_isAuthenticated)
            return;

        if (_characterSelection.CharacterId <= 0)
        {
            UpdateStatus("Character selection failed: invalid ID");
            return;
        }

        var packet = new CharacterSelectionRequestPacket(_characterSelection.CharacterId);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus($"Selecting character ID '{_characterSelection.CharacterId}'...");
    }

    // ========== HANDLERS DE RESPOSTA ==========

    private void HandleRegistrationResponse(INetPeerAdapter peer, RegistrationResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus("Account created successfully! Attempting login...");
            
            if (_login.AutoLogin && !_loginAttempted)
                TrySendLogin();
            return;
        }

        GD.PushError($"[Menu] Registration failed: {packet.Message}");
        UpdateStatus($"Registration failed: {packet.Message}");

        // Fallback: tenta login se conta já existe
        if (_login.AutoLogin && !_loginAttempted)
        {
            UpdateStatus("Attempting login with existing account...");
            TrySendLogin();
        }
    }

    private void HandleLoginResponse(INetPeerAdapter peer, LoginResponsePacket packet)
    {
        if (!packet.Success)
        {
            GD.PushError($"[Menu] Login failed: {packet.Message}");
            UpdateStatus($"Login failed: {packet.Message}");
            _loginAttempted = false;
            return;
        }
        
        UpdateStatus($"Logged in successfully. Characters: {packet.CurrentCharacters.Length}");

        _isAuthenticated = true;
        _availableCharacters.Clear();
        _availableCharacters.AddRange(packet.CurrentCharacters);
        
        // Fluxo: Sem personagens → Cria | Com personagens → Seleciona
        if (_availableCharacters.Count == 0)
        {
            TrySendCharacterCreation();
        }
        else
        {
            // Valida CharacterId configurado
            if (_characterSelection.CharacterId > 0)
            {
                var exists = _availableCharacters.Any(c => c.Id == _characterSelection.CharacterId);
                if (!exists)
                {
                    GD.PushWarning($"[Menu] CharacterId {_characterSelection.CharacterId} not found. Using first character.");
                    _characterSelection.CharacterId = _availableCharacters[0].Id;
                }
            }
            else
            {
                // Usa primeiro personagem se não configurado
                _characterSelection.CharacterId = _availableCharacters[0].Id;
            }
            
            TrySendCharacterSelection();
        }
    }

    private void HandleCharacterCreationResponse(INetPeerAdapter peer, CharacterCreationResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus($"Character '{packet.CreatedCharacter.Name}' created!");
            _availableCharacters.Add(packet.CreatedCharacter);
            
            // Auto-seleciona personagem recém-criado
            _characterSelection.CharacterId = packet.CreatedCharacter.Id;
            TrySendCharacterSelection();
            return;
        }

        GD.PushError($"[Menu] Character creation failed: {packet.Message}");
        UpdateStatus($"Character creation failed: {packet.Message}");
    }

    private void HandleCharacterSelectionResponse(INetPeerAdapter peer, CharacterSelectionResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus("Character selected! Waiting for game data...");
            
            return;
        }

        GD.PushError($"[Menu] Character selection failed: {packet.Message}");
        UpdateStatus($"Character selection failed: {packet.Message}");
    }

    private void ProcessGameData(INetPeerAdapter peer, GameDataPacket packet)
    {
        GetNode<Button>("%LoginButton").Pressed -= TrySendLogin;
        GetNode<Button>("%RegisterButton").Pressed -= TrySendRegistration;
        
        _ = HandleGameData(peer, packet);
    }
    
    /// <summary>
    /// ✅ Handler de GameDataPacket - Salva no GameStateManager e transiciona.
    /// </summary>
    private async Task HandleGameData(INetPeerAdapter peer, GameDataPacket packet)
    {
        GD.Print($"[Menu] Received GameDataPacket - LocalPlayer: {packet.LocalPlayer.Name} (NetID: {packet.LocalPlayer.NetworkId})");
        
        UpdateStatus($"Entering game as '{packet.LocalPlayer.Name}'...");
        
        // ✅ Salva dados no GameStateManager (persiste entre cenas)
        var gameState = GameStateManager.Instance;
        gameState.LocalNetworkId = packet.LocalPlayer.NetworkId;
        gameState.CurrentGameData = packet;
        _gameDataReceived = true;

        // ✅ Transiciona para o jogo após pequeno delay
        await SceneManager.Instance.LoadGame().ConfigureAwait(false);
    }

    // ========== UTILIDADES ==========
    private void ResetState()
    {
        _isAuthenticated = false;
        _registrationAttempted = false;
        _loginAttempted = false;
        _gameDataReceived = false;
        _availableCharacters.Clear();
    }

    private void UpdateStatus(string message)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = message;
        
        GD.Print($"[Menu] {message}");
    }
}