using System;
using System.Collections.Generic;
using System.Net;
using Game.Domain.Enums;
using Game.Network.Abstractions;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using Game.Network.Packets.Simulation;
using Godot;
using GodotClient.Systems;

namespace GodotClient.Scenes.Menu;

/// <summary>
/// Controlador do menu principal com autenticação completa usando UNCONNECTED packets.
/// 
/// Fluxo com máquina de estados:
/// Idle → Login/Register → CharacterSelection → CharacterCreation/Selection → Connecting → InGame
/// 
/// Autor: MonoDevPro
/// Data: 2025-10-12 21:10:59 (Refatorado)
/// </summary>
public partial class MenuScript : Control
{
    // ========== ENUMS ==========
    private enum MenuState
    {
        Idle,
        Login,
        Register,
        CharacterSelection,
        CharacterCreation,
        Connecting,
        WaitingGameData,
        TransitioningToGame,
        Error
    }
    
    // ========== CAMPOS PRIVADOS ==========
    
    #region Network & Configuration
    
    private INetworkManager? _network;
    private IPEndPoint? _serverEndPoint;
    private NetworkOptions _netOptions = null!;
    
    #endregion
    
    #region State Machine
    
    private MenuState _currentState = MenuState.Idle;
    private MenuState _previousState = MenuState.Idle;
    private readonly Dictionary<MenuState, Action> _stateEnterActions = new();
    private readonly Dictionary<MenuState, Action> _stateExitActions = new();
    
    #endregion
    
    #region Authentication State
    
    private string? _sessionToken; // Token de sessão (menu)
    private string? _gameToken;    // Token de jogo (para conectar)
    private readonly List<PlayerCharData> _availableCharacters = new();
    
    #endregion
    
    #region Configurations
    
    private LoginConfiguration _login = null!;
    private CharacterSelectionConfiguration _characterSelection = null!;
    
    #endregion
    
    #region UI Components
    
    // Status
    private Label? _statusLabel;
    
    // Login Window
    private Window _loginWindow = null!;
    private LineEdit _loginUserLineEdit = null!;
    private LineEdit _loginPassLineEdit = null!;
    private Button _loginButton = null!;
    private Button _openRegisterButton = null!;
    
    // Register Window
    private Window _registerWindow = null!;
    private LineEdit _registerUserLineEdit = null!;
    private LineEdit _registerPassLineEdit = null!;
    private LineEdit _registerPassConfirmLineEdit = null!;
    private LineEdit _registerEmailLineEdit = null!;
    private Button _registerButton = null!;
    
    // Character Selection Window
    private Window _characterSelectionWindow = null!;
    private ItemList _characterList = null!;
    private Button _selectCharacterButton = null!;
    private Button _createCharacterButton = null!;
    private Button _deleteCharacterButton = null!;
    
    // Character Creation Window
    private Window _characterCreationWindow = null!;
    private LineEdit _characterNameLineEdit = null!;
    private OptionButton _genderOptionButton = null!;
    private OptionButton _vocationOptionButton = null!;
    private Button _createButton = null!;
    
    // Global
    private Button _exitButton = null!;
    
    #endregion
    
    // ========== LIFECYCLE ==========
    
    public override void _Ready()
    {
        base._Ready();
        
        CreateStatusLabel();
        LoadConfigurations();
        LoadUIComponents();
        InitializeStateMachine();
        InitializeNetwork();
        
        // Start in Idle state, then transition to Login
        TransitionToState(MenuState.Login);
        
        GD.Print("[Menu] Initialized successfully");
    }
    
    public override void _ExitTree()
    {
        base._ExitTree();
        
        UnloadUIComponents();
        UnloadNetworkHandlers();
        
        GD.Print("[Menu] Cleanup completed");
    }
    
    // ========== STATE MACHINE ==========
    
    private void InitializeStateMachine()
    {
        // Register state enter actions
        _stateEnterActions[MenuState.Idle] = OnEnterIdle;
        _stateEnterActions[MenuState.Login] = OnEnterLogin;
        _stateEnterActions[MenuState.Register] = OnEnterRegister;
        _stateEnterActions[MenuState.CharacterSelection] = OnEnterCharacterSelection;
        _stateEnterActions[MenuState.CharacterCreation] = OnEnterCharacterCreation;
        _stateEnterActions[MenuState.Connecting] = OnEnterConnecting;
        _stateEnterActions[MenuState.WaitingGameData] = OnEnterWaitingGameData;
        _stateEnterActions[MenuState.TransitioningToGame] = OnEnterTransitioningToGame;
        _stateEnterActions[MenuState.Error] = OnEnterError;
        
        // Register state exit actions
        _stateExitActions[MenuState.Idle] = OnExitIdle;
        _stateExitActions[MenuState.Login] = OnExitLogin;
        _stateExitActions[MenuState.Register] = OnExitRegister;
        _stateExitActions[MenuState.CharacterSelection] = OnExitCharacterSelection;
        _stateExitActions[MenuState.CharacterCreation] = OnExitCharacterCreation;
        _stateExitActions[MenuState.Connecting] = OnExitConnecting;
        _stateExitActions[MenuState.WaitingGameData] = OnExitWaitingGameData;
        _stateExitActions[MenuState.TransitioningToGame] = OnExitTransitioningToGame;
        _stateExitActions[MenuState.Error] = OnExitError;
        
        GD.Print("[Menu] State machine initialized");
    }
    
    private void TransitionToState(MenuState newState)
    {
        if (_currentState == newState)
        {
            GD.PushWarning($"[Menu] Already in state: {newState}");
            return;
        }
        
        GD.Print($"[Menu] State transition: {_currentState} → {newState}");
        
        // Exit current state
        if (_stateExitActions.TryGetValue(_currentState, out var exitAction))
        {
            exitAction?.Invoke();
        }
        
        _previousState = _currentState;
        _currentState = newState;
        
        // Enter new state
        if (_stateEnterActions.TryGetValue(_currentState, out var enterAction))
        {
            enterAction?.Invoke();
        }
    }
    
    // ========== STATE ENTER HANDLERS ==========
    
    private void OnEnterIdle()
    {
        UpdateStatus("Idle");
        HideAllWindows();
    }
    
    private void OnEnterLogin()
    {
        UpdateStatus("Please login");
        HideAllWindows();
        _loginWindow.Show();
        
        // Auto-login if configured
        if (_login.AutoLogin)
        {
            CallDeferred(nameof(PerformAutoLogin));
        }
    }
    
    private void OnEnterRegister()
    {
        UpdateStatus("Create new account");
        HideAllWindows();
        _registerWindow.Show();
    }
    
    private void OnEnterCharacterSelection()
    {
        UpdateStatus($"Select character ({_availableCharacters.Count} available)");
        UpdateCharacterList();
        HideAllWindows();
        _characterSelectionWindow.Show();
        
        // Auto-select if configured
        if (_login.AutoLogin && _availableCharacters.Count > 0)
        {
            CallDeferred(nameof(PerformAutoCharacterSelection));
        }
    }
    
    private void OnEnterCharacterCreation()
    {
        UpdateStatus("Create new character");
        HideAllWindows();
        _characterCreationWindow.Show();
        
        // Clear fields
        _characterNameLineEdit.Text = "";
        _genderOptionButton.Selected = 0;
        _vocationOptionButton.Selected = 0;
    }
    
    private void OnEnterConnecting()
    {
        UpdateStatus("Connecting to game server...");
        HideAllWindows();
        
        if (_network != null && !string.IsNullOrWhiteSpace(_gameToken))
        {
            NetworkClient.Instance.NetworkManager.ConnectToServer();
        }
        else
        {
            GD.PushError("[Menu] Cannot connect: missing network or game token");
            TransitionToState(MenuState.Error);
        }
    }
    
    private void OnEnterWaitingGameData()
    {
        UpdateStatus("Authenticating with game server...");
        HideAllWindows();
        
        // Send GameConnectPacket with game token
        if (!string.IsNullOrWhiteSpace(_gameToken))
        {
            var packet = new GameConnectPacket(_gameToken);
            _network?.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            
            GD.Print($"[Menu] Sent GameConnectPacket with token: {_gameToken}");
        }
        else
        {
            GD.PushError("[Menu] No game token available!");
            TransitionToState(MenuState.Error);
        }
    }
    
    private void OnEnterTransitioningToGame()
    {
        UpdateStatus("Loading game world...");
        HideAllWindows();
        CallDeferred(nameof(PerformGameTransition));
    }
    
    private void OnEnterError()
    {
        UpdateStatus("Error occurred. Returning to login...");
        HideAllWindows();
        
        // Reset and return to login after delay
        CallDeferred(nameof(ReturnToLoginAfterError));
    }
    
    // ========== STATE EXIT HANDLERS ==========
    
    private void OnExitIdle() { }
    
    private void OnExitLogin()
    {
        _loginWindow.Hide();
    }
    
    private void OnExitRegister()
    {
        _registerWindow.Hide();
    }
    
    private void OnExitCharacterSelection()
    {
        _characterSelectionWindow.Hide();
    }
    
    private void OnExitCharacterCreation()
    {
        _characterCreationWindow.Hide();
    }
    
    private void OnExitConnecting() { }
    
    private void OnExitWaitingGameData() { }
    
    private void OnExitTransitioningToGame() { }
    
    private void OnExitError() { }
    
    // ========== STATE UTILITIES ==========
    
    private bool IsInState(MenuState state) => _currentState == state;
    
    private bool CanTransitionFrom(params MenuState[] validStates)
    {
        foreach (var state in validStates)
        {
            if (_currentState == state)
                return true;
        }
        
        GD.PushWarning($"[Menu] Invalid transition attempt from {_currentState}");
        return false;
    }
    
    private void HideAllWindows()
    {
        _loginWindow.Hide();
        _registerWindow.Hide();
        _characterSelectionWindow.Hide();
        _characterCreationWindow.Hide();
    }
    
    // ========== AUTO ACTIONS ==========
    
    private void PerformAutoLogin()
    {
        if (!IsInState(MenuState.Login))
            return;
        
        OnLoginButtonPressed();
    }
    
    private void PerformAutoCharacterSelection()
    {
        if (!IsInState(MenuState.CharacterSelection))
            return;
        
        var characterId = _characterSelection.CharacterId;
        if (characterId > 0 && characterId <= _availableCharacters.Count)
        {
            _characterList.Select(characterId - 1);
        }
        else if (_availableCharacters.Count > 0)
        {
            _characterList.Select(0);
        }
        
        OnSelectCharacterButtonPressed();
    }
    
    private async void ReturnToLoginAfterError()
    {
        await ToSignal(GetTree().CreateTimer(2.0f), SceneTreeTimer.SignalName.Timeout);
        ResetAuthenticationState();
        TransitionToState(MenuState.Login);
    }
    
    private async void PerformGameTransition()
    {
        await ToSignal(GetTree().CreateTimer(0.5f), SceneTreeTimer.SignalName.Timeout);
        await SceneManager.Instance.LoadGame().ConfigureAwait(false);
    }
    
    // ========== INITIALIZATION ==========
    
    private void CreateStatusLabel()
    {
        var statusLayer = new CanvasLayer { Name = "StatusLayer" };
        
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
        _characterSelection = configManager.GetCharacterSelectionConfiguration();
        
        GD.Print($"[Menu] Configurations loaded - Server: {_netOptions.ServerAddress}:{_netOptions.ServerPort}");
    }
    
    private void LoadUIComponents()
    {
        // Login Window
        _loginWindow = GetNode<Window>("%LoginWindow");
        _loginUserLineEdit = GetNode<LineEdit>("%LoginUserLineEdit");
        _loginPassLineEdit = GetNode<LineEdit>("%LoginPassLineEdit");
        _loginButton = GetNode<Button>("%LoginButton");
        _openRegisterButton = GetNode<Button>("%OpenRegisterButton");
        
        // Register Window
        _registerWindow = GetNode<Window>("%RegisterWindow");
        _registerUserLineEdit = GetNode<LineEdit>("%RegisterUserLineEdit");
        _registerPassLineEdit = GetNode<LineEdit>("%RegisterPassLineEdit");
        _registerPassConfirmLineEdit = GetNode<LineEdit>("%RegisterPassConfirmLineEdit");
        _registerEmailLineEdit = GetNode<LineEdit>("%RegisterEmailLineEdit");
        _registerButton = GetNode<Button>("%RegisterButton");
        
        // Character Selection Window
        _characterSelectionWindow = GetNode<Window>("%CharacterSelectionWindow");
        _characterList = GetNode<ItemList>("%CharacterList");
        _selectCharacterButton = GetNode<Button>("%SelectCharacterButton");
        _createCharacterButton = GetNode<Button>("%CreateCharacterButton");
        _deleteCharacterButton = GetNode<Button>("%DeleteCharacterButton");
        
        // Character Creation Window
        _characterCreationWindow = GetNode<Window>("%CharacterCreationWindow");
        _characterNameLineEdit = GetNode<LineEdit>("%CharacterNameLineEdit");
        _genderOptionButton = GetNode<OptionButton>("%GenderOptionButton");
        _vocationOptionButton = GetNode<OptionButton>("%VocationOptionButton");
        _createButton = GetNode<Button>("%CreateButton");
        
        // Global
        _exitButton = GetNode<Button>("%ExitButton");
        
        // Connect signals
        ConnectUISignals();
        
        // Populate dropdowns
        PopulateDropdowns();
        
        GD.Print("[Menu] UI components loaded");
    }
    
    private void ConnectUISignals()
    {
        // Login
        _loginWindow.CloseRequested += OnExitButtonPressed;
        _loginButton.Pressed += OnLoginButtonPressed;
        _openRegisterButton.Pressed += OnOpenRegisterPressed;
        
        // Register
        _registerWindow.CloseRequested += OnRegisterWindowClosed;
        _registerButton.Pressed += OnRegisterButtonPressed;
        
        // Character Selection
        _characterSelectionWindow.CloseRequested += OnCharacterSelectionWindowClosed;
        _selectCharacterButton.Pressed += OnSelectCharacterButtonPressed;
        _createCharacterButton.Pressed += OnCreateCharacterPressed;
        _deleteCharacterButton.Pressed += OnDeleteCharacterButtonPressed;
        
        // Character Creation
        _characterCreationWindow.CloseRequested += OnCharacterCreationWindowClosed;
        _createButton.Pressed += OnCreateCharacterButtonPressed;
        
        // Global
        _exitButton.Pressed += OnExitButtonPressed;
    }
    
    private void PopulateDropdowns()
    {
        // Gender
        _genderOptionButton.Clear();
        foreach (var gender in Enum.GetValues<Gender>())
        {
            _genderOptionButton.AddItem(gender.ToString());
        }
        
        // Vocation
        _vocationOptionButton.Clear();
        foreach (var vocation in Enum.GetValues<VocationType>())
        {
            _vocationOptionButton.AddItem(vocation.ToString());
        }
    }
    
    private void InitializeNetwork()
    {
        // Create server endpoint
        _serverEndPoint = new IPEndPoint(
            IPAddress.Parse(_netOptions.ServerAddress),
            _netOptions.ServerPort
        );
        
        // Initialize network manager
        _network = NetworkClient.Instance.Initialize(_netOptions);
        
        // Register connection events
        _network.OnPeerConnected += OnPeerConnected;
        _network.OnPeerDisconnected += OnPeerDisconnected;
        
        // Register UNCONNECTED packet handlers (menu/authentication)
        RegisterUnconnectedHandlers();
        
        // Register CONNECTED packet handlers (game data)
        RegisterConnectedHandlers();
        
        // Start network (listening for unconnected messages)
        NetworkClient.Instance.Start();
        
        GD.Print("[Menu] Network initialized");
    }
    
    private void RegisterUnconnectedHandlers()
    {
        _network!.RegisterUnconnectedPacketHandler<UnconnectedLoginResponsePacket>(HandleLoginResponse);
        _network.RegisterUnconnectedPacketHandler<UnconnectedRegistrationResponsePacket>(HandleRegistrationResponse);
        _network.RegisterUnconnectedPacketHandler<UnconnectedCharacterCreationResponsePacket>(HandleCharacterCreationResponse);
        _network.RegisterUnconnectedPacketHandler<UnconnectedCharacterSelectionResponsePacket>(HandleCharacterSelectionResponse);
        _network.RegisterUnconnectedPacketHandler<UnconnectedGameTokenResponsePacket>(HandleGameTokenResponse);
    }
    
    private void RegisterConnectedHandlers()
    {
        _network!.RegisterPacketHandler<GameDataPacket>(HandleGameData);
    }
    
    // ========== CLEANUP ==========
    
    private void UnloadUIComponents()
    {
        _loginButton.Pressed -= OnLoginButtonPressed;
        _openRegisterButton.Pressed -= OnOpenRegisterPressed;
        _registerButton.Pressed -= OnRegisterButtonPressed;
        _selectCharacterButton.Pressed -= OnSelectCharacterButtonPressed;
        _createCharacterButton.Pressed -= OnCreateCharacterPressed;
        _deleteCharacterButton.Pressed -= OnDeleteCharacterButtonPressed;
        _createButton.Pressed -= OnCreateCharacterButtonPressed;
        _exitButton.Pressed -= OnExitButtonPressed;
    }
    
    private void UnloadNetworkHandlers()
    {
        if (_network is null) return;
        
        // Remove connection events
        _network.OnPeerConnected -= OnPeerConnected;
        _network.OnPeerDisconnected -= OnPeerDisconnected;
        
        // Unregister UNCONNECTED handlers
        _network.UnregisterUnconnectedPacketHandler<UnconnectedLoginResponsePacket>();
        _network.UnregisterUnconnectedPacketHandler<UnconnectedRegistrationResponsePacket>();
        _network.UnregisterUnconnectedPacketHandler<UnconnectedCharacterCreationResponsePacket>();
        _network.UnregisterUnconnectedPacketHandler<UnconnectedCharacterSelectionResponsePacket>();
        _network.UnregisterUnconnectedPacketHandler<UnconnectedGameTokenResponsePacket>();
        
        // Unregister CONNECTED handlers
        _network.UnregisterPacketHandler<GameDataPacket>();
    }
    
    // ========== UI EVENTS ==========
    
    private void OnOpenRegisterPressed()
    {
        if (CanTransitionFrom(MenuState.Login))
        {
            TransitionToState(MenuState.Register);
        }
    }
    
    private void OnRegisterWindowClosed()
    {
        if (IsInState(MenuState.Register))
        {
            TransitionToState(MenuState.Login);
        }
    }
    
    private void OnCharacterSelectionWindowClosed()
    {
        if (IsInState(MenuState.CharacterSelection))
        {
            ResetAuthenticationState();
            TransitionToState(MenuState.Login);
        }
    }
    
    private void OnCreateCharacterPressed()
    {
        if (CanTransitionFrom(MenuState.CharacterSelection))
        {
            TransitionToState(MenuState.CharacterCreation);
        }
    }
    
    private void OnCharacterCreationWindowClosed()
    {
        if (IsInState(MenuState.CharacterCreation))
        {
            TransitionToState(MenuState.CharacterSelection);
        }
    }
    
    private void OnLoginButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.Login))
            return;
        
        if (_network is null || _serverEndPoint is null)
        {
            UpdateStatus("Error: Network not initialized");
            return;
        }
        
        var username = _loginUserLineEdit.Text.Trim();
        var password = _loginPassLineEdit.Text.Trim();
        
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            UpdateStatus("Login failed: credentials missing");
            return;
        }
        
        var packet = new UnconnectedLoginRequestPacket(username, password);
        _network.SendUnconnected(_serverEndPoint, packet);
        
        UpdateStatus($"Authenticating as '{username}'...");
        GD.Print($"[Menu] Login request sent for: {username}");
    }
    
    private void OnRegisterButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.Register))
            return;
        
        if (_network is null || _serverEndPoint is null)
        {
            UpdateStatus("Error: Network not initialized");
            return;
        }
        
        var username = _registerUserLineEdit.Text.Trim();
        var password = _registerPassLineEdit.Text.Trim();
        var passwordConfirm = _registerPassConfirmLineEdit.Text.Trim();
        var email = _registerEmailLineEdit.Text.Trim();
        
        // Validation
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            UpdateStatus("Registration failed: username or password missing");
            return;
        }
        
        if (password != passwordConfirm)
        {
            UpdateStatus("Registration failed: passwords do not match");
            return;
        }
        
        if (string.IsNullOrWhiteSpace(email))
        {
            UpdateStatus("Registration failed: email missing");
            return;
        }
        
        var packet = new UnconnectedRegistrationRequestPacket(username, email, password);
        _network.SendUnconnected(_serverEndPoint, packet);
        
        UpdateStatus($"Registering user '{username}'...");
        GD.Print($"[Menu] Registration request sent for: {username}");
    }
    
    private void OnSelectCharacterButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.CharacterSelection))
            return;
        
        if (_network is null || _serverEndPoint is null || string.IsNullOrWhiteSpace(_sessionToken))
        {
            UpdateStatus("Error: Invalid session");
            return;
        }
        
        var selectedIndices = _characterList.GetSelectedItems();
        
        if (selectedIndices.Length == 0)
        {
            UpdateStatus("Character selection failed: no character selected");
            return;
        }
        
        var selectedIndex = selectedIndices[0];
        
        if (selectedIndex < 0 || selectedIndex >= _availableCharacters.Count)
        {
            UpdateStatus("Character selection failed: invalid selection");
            return;
        }
        
        var characterId = _availableCharacters[selectedIndex].Id;
        
        var packet = new UnconnectedCharacterSelectionRequestPacket(_sessionToken, characterId);
        _network.SendUnconnected(_serverEndPoint, packet);
        
        UpdateStatus($"Selecting character ID '{characterId}'...");
        GD.Print($"[Menu] Character selection request sent: ID {characterId}");
    }
    
    private void OnCreateCharacterButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.CharacterCreation))
            return;
        
        if (_network is null || _serverEndPoint is null || string.IsNullOrWhiteSpace(_sessionToken))
        {
            UpdateStatus("Error: Invalid session");
            return;
        }
        
        var name = _characterNameLineEdit.Text.Trim();
        var genderIndex = _genderOptionButton.Selected;
        var vocationIndex = _vocationOptionButton.Selected;
        
        // Validation
        if (string.IsNullOrWhiteSpace(name))
        {
            UpdateStatus("Character creation failed: no name specified");
            return;
        }
        
        if (genderIndex < 0 || genderIndex >= Enum.GetValues<Gender>().Length)
        {
            UpdateStatus("Character creation failed: invalid gender");
            return;
        }
        
        if (vocationIndex < 0 || vocationIndex >= Enum.GetValues<VocationType>().Length)
        {
            UpdateStatus("Character creation failed: invalid vocation");
            return;
        }
        
        var gender = (Gender)genderIndex;
        var vocation = (VocationType)vocationIndex;
        
        var packet = new UnconnectedCharacterCreationRequestPacket(_sessionToken, name, gender, vocation);
        _network.SendUnconnected(_serverEndPoint, packet);
        
        UpdateStatus($"Creating character '{name}'...");
        GD.Print($"[Menu] Character creation request sent: {name} ({gender}, {vocation})");
    }
    
    private void OnDeleteCharacterButtonPressed()
    {
        GD.PushWarning("[Menu] Delete character feature not implemented yet");
        UpdateStatus("Delete character: Not implemented");
    }
    
    private void OnExitButtonPressed()
    {
        GD.Print("[Menu] Exiting game...");
        GetTree().Quit();
    }
    
    // ========== NETWORK EVENTS ==========
    
    private void OnPeerConnected(INetPeerAdapter peer)
    {
        GD.Print($"[Menu] Connected to server (Peer: {peer.Id})");
        
        if (IsInState(MenuState.Connecting))
        {
            TransitionToState(MenuState.WaitingGameData);
        }
    }
    
    private void OnPeerDisconnected(INetPeerAdapter peer)
    {
        GD.PushWarning("[Menu] Disconnected from server");
        
        if (!IsInState(MenuState.TransitioningToGame))
        {
            UpdateStatus("Disconnected from server");
            TransitionToState(MenuState.Error);
        }
    }
    
    // ========== PACKET HANDLERS (UNCONNECTED) ==========
    
    private void HandleLoginResponse(IPEndPoint remoteEndPoint, UnconnectedLoginResponsePacket packet)
    {
        if (!packet.Success)
        {
            GD.PushError($"[Menu] Login failed: {packet.Message}");
            UpdateStatus($"Login failed: {packet.Message}");
            return;
        }
        
        _sessionToken = packet.SessionToken;
        _availableCharacters.Clear();
        _availableCharacters.AddRange(packet.CurrentCharacters);
        
        GD.Print($"[Menu] Login successful - Session token: {_sessionToken}, Characters: {_availableCharacters.Count}");
        
        TransitionToState(MenuState.CharacterSelection);
    }
    
    private void HandleRegistrationResponse(IPEndPoint remoteEndPoint, UnconnectedRegistrationResponsePacket packet)
    {
        if (packet.Success)
        {
            GD.Print("[Menu] Registration successful");
            UpdateStatus("Account created successfully!");
            TransitionToState(MenuState.Login);
            return;
        }
        
        GD.PushError($"[Menu] Registration failed: {packet.Message}");
        UpdateStatus($"Registration failed: {packet.Message}");
    }
    
    private void HandleCharacterCreationResponse(IPEndPoint remoteEndPoint, UnconnectedCharacterCreationResponsePacket packet)
    {
        if (packet.Success)
        {
            _availableCharacters.Add(packet.CreatedCharacter);
            GD.Print($"[Menu] Character created: {packet.CreatedCharacter.Name}");
            UpdateStatus($"Character '{packet.CreatedCharacter.Name}' created!");
            TransitionToState(MenuState.CharacterSelection);
            return;
        }
        
        GD.PushError($"[Menu] Character creation failed: {packet.Message}");
        UpdateStatus($"Character creation failed: {packet.Message}");
    }
    
    private void HandleCharacterSelectionResponse(IPEndPoint remoteEndPoint, UnconnectedCharacterSelectionResponsePacket packet)
    {
        if (packet.Success)
        {
            GD.Print($"[Menu] Character selected: ID {packet.CharacterId}");
            UpdateStatus("Character selected! Waiting for game token...");
            return;
        }
        
        GD.PushError($"[Menu] Character selection failed: {packet.Message}");
        UpdateStatus($"Character selection failed: {packet.Message}");
    }
    
    private void HandleGameTokenResponse(IPEndPoint remoteEndPoint, UnconnectedGameTokenResponsePacket packet)
    {
        _gameToken = packet.GameToken;
        
        GD.Print($"[Menu] Game token received: {_gameToken}");
        
        TransitionToState(MenuState.Connecting);
    }
    
    // ========== PACKET HANDLERS (CONNECTED) ==========
    
    private void HandleGameData(INetPeerAdapter peer, GameDataPacket packet)
    {
        GD.Print($"[Menu] Received GameDataPacket - Player: {packet.LocalPlayer.Name} (NetID: {packet.LocalPlayer.NetworkId})");
        
        // Save to GameStateManager (persists across scenes)
        var gameState = GameStateManager.Instance;
        gameState.LocalNetworkId = packet.LocalPlayer.NetworkId;
        gameState.CurrentGameData = packet;
        
        TransitionToState(MenuState.TransitioningToGame);
    }
    
    // ========== UTILITIES ==========
    
    private void UpdateCharacterList()
    {
        _characterList.Clear();
        
        foreach (var character in _availableCharacters)
        {
            var itemText = $"{character.Name} - Lv.{character.Level} {character.Vocation} ({character.Gender})";
            _characterList.AddItem(itemText);
        }
        
        GD.Print($"[Menu] Character list updated - {_availableCharacters.Count} characters");
    }
    
    private void ResetAuthenticationState()
    {
        _sessionToken = null;
        _gameToken = null;
        _availableCharacters.Clear();
        
        GD.Print("[Menu] Authentication state reset");
    }
    
    private void UpdateStatus(string message)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = $"[{_currentState}] {message}";
        }
        
        GD.Print($"[Menu] [{_currentState}] {message}");
    }
}