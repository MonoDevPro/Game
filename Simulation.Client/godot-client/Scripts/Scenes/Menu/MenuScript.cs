using System.Collections.Generic;
using System.Linq;
using System.Net;
using Game.Network.Abstractions;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using Game.Network.Packets.Simulation;
using Godot;
using GodotClient.Systems;

namespace GodotClient.Scenes.Menu;

/// <summary>
/// Controlador do menu com autenticação completa usando UNCONNECTED packets.
/// Autor: MonoDevPro
/// Data: 2025-01-12 06:54:48
/// </summary>
public partial class MenuScript : Control
{
    private INetworkManager? _network;
    private IPEndPoint? _serverEndPoint;
    private Label? _statusLabel;
    
    // Configurações
    private NetworkOptions _netOptions = null!;
    private LoginConfiguration _login = null!;
    private RegistrationConfiguration _registration = null!;
    private CharacterCreationConfiguration _characterCreation = null!;
    private CharacterSelectionConfiguration _characterSelection = null!;
    
    // ✅ Estado de autenticação COM TOKENS
    private string? _sessionToken;
    private string? _gameToken; // ✅ ADICIONADO
    private bool _registrationAttempted;
    private bool _loginAttempted;
    private bool _gameDataReceived;
    private bool _isConnecting; // ✅ ADICIONADO
    
    // ✅ Cache de personagens disponíveis
    private readonly List<PlayerCharData> _availableCharacters = new();
    
    // UI Components
    private Window _loginWindow = null!;
    private Window _registerWindow = null!;
    private Button _loginButton = null!;
    private Button _registerButton = null!;
    private Button _openRegisterButton = null!;
    private Button _exitButton = null!;

    public override void _Ready()
    {
        base._Ready();

        CreateStatusLabel();
        LoadConfigurations();
        LoadMenuComponents();
        InitializeNetwork();
    }

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
    
    private void LoadMenuComponents()
    {
        _loginWindow = GetNode<Window>("%LoginWindow");
        _registerWindow = GetNode<Window>("%RegisterWindow");
        _loginButton = GetNode<Button>("%LoginButton");
        _registerButton = GetNode<Button>("%RegisterButton");
        _openRegisterButton = GetNode<Button>("%OpenRegisterButton");
        _exitButton = GetNode<Button>("%ExitButton");

        _loginButton.Pressed += TrySendLogin;
        _registerButton.Pressed += TrySendRegistration;
        _openRegisterButton.Pressed += _registerWindow.Show;
        _exitButton.Pressed += ExitGame;
        
        UpdateMenuComponents();
    }
    
    private void UpdateMenuComponents()
    {
        _loginButton.Disabled = _loginAttempted || _gameDataReceived;
        _registerButton.Disabled = _registrationAttempted || _gameDataReceived;
        _openRegisterButton.Disabled = _loginAttempted || _gameDataReceived;
        _exitButton.Disabled = _loginAttempted || _gameDataReceived;
    }

    /// <summary>
    /// ✅ Inicializa rede SEM CONECTAR - apenas prepara para unconnected.
    /// </summary>
    private void InitializeNetwork()
    {
        UpdateStatus($"Network Options: Server={_netOptions.ServerAddress}:{_netOptions.ServerPort}");
        
        // ✅ Cria endpoint do servidor
        _serverEndPoint = new IPEndPoint(
            IPAddress.Parse(_netOptions.ServerAddress),
            _netOptions.ServerPort
        );
        
        // ✅ Inicializa network manager
        _network = NetworkClient.Instance.Initialize(_netOptions);

        // ✅ Eventos de conexão
        _network.OnPeerConnected += OnPeerConnected;
        _network.OnPeerDisconnected += OnPeerDisconnected;

        // ✅ Registra handlers UNCONNECTED
        _network.RegisterUnconnectedPacketHandler<UnconnectedLoginResponsePacket>(HandleLoginResponse);
        _network.RegisterUnconnectedPacketHandler<UnconnectedRegistrationResponsePacket>(HandleRegistrationResponse);
        _network.RegisterUnconnectedPacketHandler<UnconnectedCharacterCreationResponsePacket>(HandleCharacterCreationResponse);
        _network.RegisterUnconnectedPacketHandler<UnconnectedCharacterSelectionResponsePacket>(HandleCharacterSelectionResponse);
        _network.RegisterUnconnectedPacketHandler<UnconnectedGameTokenResponsePacket>(HandleGameTokenResponse); // ✅ ADICIONADO
        
        // ✅ Registra handler CONNECTED (GameData vem após conexão)
        _network.RegisterPacketHandler<GameDataPacket>(HandleGameData); // ✅ CORRIGIDO
        
        // ✅ Inicia network manager (para receber unconnected)
        NetworkClient.Instance.Start();
        
        UpdateStatus("Network initialized. Ready to authenticate.");
        
        // ✅ Auto-register/login se configurado
        if (_registration.AutoRegister && !_registrationAttempted)
        {
            TrySendRegistration();
        }
        else if (_login.AutoLogin && !_loginAttempted)
        {
            TrySendLogin();
        }
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        UnloadMenuComponents();

        if (_network is not null)
        {
            // ✅ Remove eventos
            _network.OnPeerConnected -= OnPeerConnected;
            _network.OnPeerDisconnected -= OnPeerDisconnected;

            // ✅ Desregistra handlers unconnected
            _network.UnregisterUnconnectedPacketHandler<UnconnectedLoginResponsePacket>();
            _network.UnregisterUnconnectedPacketHandler<UnconnectedRegistrationResponsePacket>();
            _network.UnregisterUnconnectedPacketHandler<UnconnectedCharacterCreationResponsePacket>();
            _network.UnregisterUnconnectedPacketHandler<UnconnectedCharacterSelectionResponsePacket>();
            _network.UnregisterUnconnectedPacketHandler<UnconnectedGameTokenResponsePacket>(); // ✅ ADICIONADO
            
            // ✅ Desregistra handler connected
            _network.UnregisterPacketHandler<GameDataPacket>();
        }
        
        GD.Print("[Menu] Unloaded");
    }

    private void UnloadMenuComponents()
    {
        _loginButton.Pressed -= TrySendLogin;
        _registerButton.Pressed -= TrySendRegistration;
        _openRegisterButton.Pressed -= _registerWindow.Show;
        _exitButton.Pressed -= ExitGame;
    }

    // ========== EVENTOS DE REDE ==========

    /// <summary>
    /// ✅ Chamado quando conecta ao servidor (após receber game token).
    /// </summary>
    private void OnPeerConnected(INetPeerAdapter peer)
    {
        GD.Print($"[Menu] Connected to server (Peer: {peer.Id})");
        UpdateStatus("Connected! Authenticating...");

        // ✅ Envia GameConnectPacket com o game token
        if (!string.IsNullOrWhiteSpace(_gameToken))
        {
            var packet = new GameConnectPacket(_gameToken);
            _network?.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            
            GD.Print($"[Menu] Sent GameConnectPacket with token: {_gameToken}");
            UpdateStatus("Sent authentication token. Waiting for game data...");
        }
        else
        {
            GD.PushError("[Menu] Connected but no game token available!");
            UpdateStatus("Error: No game token");
        }
    }

    private void OnPeerDisconnected(INetPeerAdapter peer)
    {
        GD.PushWarning("[Menu] Disconnected from server");
        UpdateStatus("Disconnected from server");
        
        if (!_gameDataReceived)
        {
            // ✅ Se desconectou antes de receber game data, reseta estado
            ResetState();
        }
    }

    // ========== ENVIO DE PACOTES UNCONNECTED ==========

    private void TrySendRegistration()
    {
        if (_network is null || _serverEndPoint is null || _registrationAttempted)
            return;
        
        _registration.Username = GetNode<LineEdit>("%RegisterUserLineEdit").Text.Trim();
        _registration.Password = GetNode<LineEdit>("%RegisterPassLineEdit").Text.Trim();
        _registration.Email = GetNode<LineEdit>("%RegisterEmailLineEdit").Text.Trim();
        
        var registerConfirm = GetNode<LineEdit>("%RegisterPassConfirmLineEdit").Text.Trim();
        if (_registration.Password != registerConfirm)
        {
            UpdateStatus("Registration failed: passwords do not match");
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
            UpdateStatus("Registration failed: username or password missing");
            _registrationAttempted = false;
            return;
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            UpdateStatus("Registration failed: email missing");
            _registrationAttempted = false;
            return;
        }

        var packet = new UnconnectedRegistrationRequestPacket(username, email, password);
        _network.SendUnconnected(_serverEndPoint, packet);
        
        UpdateStatus($"Registering user '{username}'...");
        
        _registerWindow?.Hide();
        _loginWindow?.Show();
    }

    private void TrySendLogin()
    {
        if (_network is null || _serverEndPoint is null || _loginAttempted)
            return;
        
        _login.Username = GetNode<LineEdit>("%LoginUserLineEdit").Text.Trim();
        _login.Password = GetNode<LineEdit>("%LoginPassLineEdit").Text.Trim();

        if (string.IsNullOrWhiteSpace(_login.Username) || string.IsNullOrWhiteSpace(_login.Password))
        {
            UpdateStatus("Login failed: credentials missing");
            return;
        }

        var packet = new UnconnectedLoginRequestPacket(_login.Username, _login.Password);
        _network.SendUnconnected(_serverEndPoint, packet);
        
        UpdateStatus($"Authenticating as '{_login.Username}'...");
        _loginAttempted = true;
    }

    private void TrySendCharacterCreation()
    {
        if (_network is null || _serverEndPoint is null || string.IsNullOrWhiteSpace(_sessionToken))
            return;

        if (string.IsNullOrWhiteSpace(_characterCreation.Name))
        {
            UpdateStatus("Character creation failed: no name specified");
            return;
        }

        var packet = new UnconnectedCharacterCreationRequestPacket(
            _sessionToken,
            _characterCreation.Name,
            _characterCreation.Gender,
            _characterCreation.Vocation);
        
        _network.SendUnconnected(_serverEndPoint, packet);
        UpdateStatus($"Creating character '{_characterCreation.Name}'...");
    }

    private void TrySendCharacterSelection()
    {
        if (_network is null || _serverEndPoint is null || string.IsNullOrWhiteSpace(_sessionToken))
            return;

        if (_characterSelection.CharacterId <= 0)
        {
            UpdateStatus("Character selection failed: invalid ID");
            return;
        }

        var packet = new UnconnectedCharacterSelectionRequestPacket(
            _sessionToken,
            _characterSelection.CharacterId);
        
        _network.SendUnconnected(_serverEndPoint, packet);
        UpdateStatus($"Selecting character ID '{_characterSelection.CharacterId}'...");
    }

    // ========== HANDLERS DE RESPOSTA UNCONNECTED ==========

    private void HandleRegistrationResponse(IPEndPoint remoteEndPoint, UnconnectedRegistrationResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus("Account created successfully! Attempting login...");
            
            if (_login.AutoLogin && !_loginAttempted)
            {
                TrySendLogin();
            }
            return;
        }

        GD.PushError($"[Menu] Registration failed: {packet.Message}");
        UpdateStatus($"Registration failed: {packet.Message}");
        _registrationAttempted = false;

        if (_login.AutoLogin && !_loginAttempted && packet.Message.Contains("already exists"))
        {
            UpdateStatus("Attempting login with existing account...");
            TrySendLogin();
        }
    }

    private void HandleLoginResponse(IPEndPoint remoteEndPoint, UnconnectedLoginResponsePacket packet)
    {
        if (!packet.Success)
        {
            GD.PushError($"[Menu] Login failed: {packet.Message}");
            UpdateStatus($"Login failed: {packet.Message}");
            _loginAttempted = false;
            return;
        }
        
        _sessionToken = packet.SessionToken;
        
        UpdateStatus($"Logged in successfully. Characters: {packet.CurrentCharacters.Length}");
        GD.Print($"[Menu] Session token received: {_sessionToken}");

        _availableCharacters.Clear();
        _availableCharacters.AddRange(packet.CurrentCharacters);
        
        if (_availableCharacters.Count == 0)
        {
            TrySendCharacterCreation();
        }
        else
        {
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
                _characterSelection.CharacterId = _availableCharacters[0].Id;
            }
            
            TrySendCharacterSelection();
        }
    }

    private void HandleCharacterCreationResponse(IPEndPoint remoteEndPoint, UnconnectedCharacterCreationResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus($"Character '{packet.CreatedCharacter.Name}' created!");
            _availableCharacters.Add(packet.CreatedCharacter);
            
            _characterSelection.CharacterId = packet.CreatedCharacter.Id;
            TrySendCharacterSelection();
            return;
        }

        GD.PushError($"[Menu] Character creation failed: {packet.Message}");
        UpdateStatus($"Character creation failed: {packet.Message}");
    }

    private void HandleCharacterSelectionResponse(IPEndPoint remoteEndPoint, UnconnectedCharacterSelectionResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus("Character selected! Waiting for game token...");
            _characterSelection.CharacterId = packet.CharacterId;
            return;
        }

        GD.PushError($"[Menu] Character selection failed: {packet.Message}");
        UpdateStatus($"Character selection failed: {packet.Message}");
    }

    /// <summary>
    /// ✅ NOVO: Handler de game token (servidor envia após seleção).
    /// </summary>
    private void HandleGameTokenResponse(IPEndPoint remoteEndPoint, UnconnectedGameTokenResponsePacket packet)
    {
        _gameToken = packet.GameToken;
        
        GD.Print($"[Menu] Game token received: {_gameToken}");
        UpdateStatus("Game token received! Connecting to server...");

        // ✅ AGORA conecta ao servidor
        if (!_isConnecting)
        {
            _isConnecting = true;
            NetworkClient.Instance.NetworkManager.ConnectToServer();
        }
    }

    /// <summary>
    /// ✅ Handler CONNECTED de GameDataPacket.
    /// </summary>
    private void HandleGameData(INetPeerAdapter peer, GameDataPacket packet)
    {
        GD.Print($"[Menu] Received GameDataPacket - LocalPlayer: {packet.LocalPlayer.Name} (NetID: {packet.LocalPlayer.NetworkId})");
        
        UpdateStatus($"Entering game as '{packet.LocalPlayer.Name}'...");
        
        var gameState = GameStateManager.Instance;
        gameState.LocalNetworkId = packet.LocalPlayer.NetworkId;
        gameState.CurrentGameData = packet;
        _gameDataReceived = true;

        UnloadMenuComponents();
        CallDeferred(nameof(TransitionToGame));
    }

    private async void TransitionToGame()
    {
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        await SceneManager.Instance.LoadGame().ConfigureAwait(false);
    }

    // ========== UTILIDADES ==========

    private void ResetState()
    {
        _sessionToken = null;
        _gameToken = null;
        _registrationAttempted = false;
        _loginAttempted = false;
        _gameDataReceived = false;
        _isConnecting = false;
        _availableCharacters.Clear();
    }

    private void UpdateStatus(string message)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = message;
        }
        GD.Print($"[Menu] {message}");
    }
    
    public void ExitGame() => GetTree().Quit();
}