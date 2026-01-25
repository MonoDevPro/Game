using System;
using System.Collections.Generic;
using Game.Contracts;
using Game.Core.Autoloads;
using Game.Domain;
using Godot;

namespace Game.UI.Menu;

/// <summary>
/// Controlador do menu principal com autenticação usando Envelope/OpCode.
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
        CharacterDeletion,
        Connecting,
        WaitingGameData,
        TransitioningToGame,
        Error
    }

    // ========== CAMPOS PRIVADOS ==========

    #region Network & Configuration

    private NetworkConfiguration _networkConfig = null!;
    private NetClientConnection _authConnection = null!;
    private NetClientConnection _worldConnection = null!;

    #endregion

    #region State Machine

    private MenuState _currentState = MenuState.Idle;
    private MenuState _previousState = MenuState.Idle;
    private readonly Dictionary<MenuState, Action> _stateEnterActions = new();
    private readonly Dictionary<MenuState, Action> _stateExitActions = new();

    #endregion

    #region Authentication State

    private string? _sessionToken; // Token de sessão (menu)

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

    // ✅ Character Deletion Confirmation Dialog
    private AcceptDialog? _deleteConfirmationDialog;

    // Global
    private Button _exitButton = null!;

    #endregion

    private readonly List<CharacterSummary> _availableCharacters = new();

    // ========== LIFECYCLE ==========

    public override void _Ready()
    {
        base._Ready();

        LoadConfigurations();

        CreateStatusLabel();
        CreateDeleteConfirmationDialog();
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
        _stateEnterActions[MenuState.CharacterDeletion] = OnEnterCharacterDeletion;
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
        _stateExitActions[MenuState.CharacterDeletion] = OnExitCharacterDeletion;
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
        UpdateCharacterList();
        HideAllWindows();
        _characterSelectionWindow.Show();
    }

    private void OnEnterCharacterCreation()
    {
        UpdateStatus("Create new character");
        HideAllWindows();
        _characterCreationWindow.Show();

        // Clear fields
        _characterNameLineEdit.Text = "";
        _genderOptionButton.Selected = -1;
        _vocationOptionButton.Selected = -1;
    }

    private void OnEnterCharacterDeletion()
    {
        HideAllWindows();

        // Mostra dialog de confirmação
        ShowDeleteConfirmationDialog();
    }

    private void OnEnterConnecting()
    {
        UpdateStatus("Connecting to game server...");
        HideAllWindows();
    }

    private void OnEnterWaitingGameData()
    {
        UpdateStatus("Authenticating with game server...");
        HideAllWindows();
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

    private void OnExitIdle()
    {
    }

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

    private void OnExitCharacterDeletion()
    {
    }

    private void OnExitConnecting()
    {
    }

    private void OnExitWaitingGameData()
    {
    }

    private void OnExitTransitioningToGame()
    {
    }

    private void OnExitError()
    {
    }

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

        // ✅ Esconder dialog de confirmação também
        if (_deleteConfirmationDialog is not null && _deleteConfirmationDialog.Visible)
            _deleteConfirmationDialog.Hide();
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
        if (characterId <= 0)
            return;

        var index = _availableCharacters.FindIndex(c => c.Id == characterId);
        if (index < 0)
            return;

        _characterList.Select(index);
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

    private void CreateDeleteConfirmationDialog()
    {
        _deleteConfirmationDialog = new AcceptDialog
        {
            Title = "Confirm Character Deletion",
            DialogText = "Are you sure you want to delete this character?\n\nThis action cannot be undone!",
            OkButtonText = "Delete",
            Exclusive = true,
            PopupWindow = false
        };

        // Conectar eventos
        _deleteConfirmationDialog.Confirmed += OnDeleteConfirmed;
        _deleteConfirmationDialog.Canceled += OnDeleteCanceled;
        _deleteConfirmationDialog.CloseRequested += OnDeleteCanceled;

        AddChild(_deleteConfirmationDialog);

        GD.Print("[Menu] Delete confirmation dialog created");
    }

    private void ShowDeleteConfirmationDialog()
    {
        if (_deleteConfirmationDialog is null)
            return;

        _deleteConfirmationDialog.DialogText =
            "This action cannot be undone!";

        _deleteConfirmationDialog.PopupCentered();
    }

    private void CleanupDeleteConfirmationDialog()
    {
        if (_deleteConfirmationDialog is null)
            return;

        _deleteConfirmationDialog.Confirmed -= OnDeleteConfirmed;
        _deleteConfirmationDialog.Canceled -= OnDeleteCanceled;
        _deleteConfirmationDialog.CloseRequested -= OnDeleteCanceled;

        _deleteConfirmationDialog.QueueFree();
        _deleteConfirmationDialog = null;

        GD.Print("[Menu] Delete confirmation dialog cleaned up");
    }

    private void LoadConfigurations()
    {
        var configManager = ConfigManager.Instance;

        _login = configManager.GetLoginConfiguration();
        _characterSelection = configManager.GetCharacterSelectionConfiguration();
        _networkConfig = configManager.Configuration.Network ?? new NetworkConfiguration();
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
        _loginUserLineEdit.TextSubmitted += OnLoginButtonPressedFromTextSubmitted;
        _loginPassLineEdit.TextSubmitted += OnLoginButtonPressedFromTextSubmitted;

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
            _genderOptionButton.AddItem(gender.ToString(), (int)gender);
        }

        // Vocation
        _vocationOptionButton.Clear();
        foreach (var vocation in Enum.GetValues<Vocation>())
        {
            _vocationOptionButton.AddItem(vocation.ToString(), (int)vocation);
        }
    }

    private void InitializeNetwork()
    {
        _authConnection = NetworkClient.Instance.AuthConnection;
        _worldConnection = NetworkClient.Instance.WorldConnection;

        _authConnection.Connected += OnAuthConnected;
        _authConnection.Disconnected += OnAuthDisconnected;
        _authConnection.EnvelopeReceived += OnAuthEnvelopeReceived;

        _worldConnection.Connected += OnWorldConnected;
        _worldConnection.Disconnected += OnWorldDisconnected;
        _worldConnection.EnvelopeReceived += OnWorldEnvelopeReceived;

        NetworkClient.Instance.Start();

        if (!_authConnection.Connect(_networkConfig.ServerAddress, _networkConfig.AuthPort))
        {
            UpdateStatus("Failed to connect to auth server");
        }

        GD.Print("[Menu] Network initialized");
    }

    // ========== CLEANUP ==========

    private void UnloadUIComponents()
    {
        _loginButton.Pressed -= OnLoginButtonPressed;
        _openRegisterButton.Pressed -= OnOpenRegisterPressed;
        _loginUserLineEdit.TextSubmitted -= OnLoginButtonPressedFromTextSubmitted;
        _loginPassLineEdit.TextSubmitted -= OnLoginButtonPressedFromTextSubmitted;
        _registerButton.Pressed -= OnRegisterButtonPressed;
        _selectCharacterButton.Pressed -= OnSelectCharacterButtonPressed;
        _createCharacterButton.Pressed -= OnCreateCharacterPressed;
        _deleteCharacterButton.Pressed -= OnDeleteCharacterButtonPressed;
        _createButton.Pressed -= OnCreateCharacterButtonPressed;
        _exitButton.Pressed -= OnExitButtonPressed;
    }

    private void UnloadNetworkHandlers()
    {
        if (_authConnection is not null)
        {
            _authConnection.Connected -= OnAuthConnected;
            _authConnection.Disconnected -= OnAuthDisconnected;
            _authConnection.EnvelopeReceived -= OnAuthEnvelopeReceived;
        }

        if (_worldConnection is not null)
        {
            _worldConnection.Connected -= OnWorldConnected;
            _worldConnection.Disconnected -= OnWorldDisconnected;
            _worldConnection.EnvelopeReceived -= OnWorldEnvelopeReceived;
        }
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

    private void OnLoginButtonPressedFromTextSubmitted(string _)
    {
        OnLoginButtonPressed();
    }

    private void OnLoginButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.Login))
            return;

        if (_authConnection is null || !_authConnection.IsConnected)
        {
            UpdateStatus("Error: Auth server not connected");
            return;
        }

        var username = _loginUserLineEdit.Text.Trim();
        var password = _loginPassLineEdit.Text.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            UpdateStatus("Login failed: credentials missing");
            return;
        }

        var request = new AuthLoginRequest(username, password);
        _authConnection.Send(new Envelope(OpCode.AuthLoginRequest, request));

        UpdateStatus($"Authenticating as '{username}'...");
        GD.Print($"[Menu] Login request sent for: {username}");
    }

    private void OnRegisterButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.Register))
            return;

        UpdateStatus("Registration not supported by this server");
    }

    private void OnSelectCharacterButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.CharacterSelection))
            return;

        if (_authConnection is null || string.IsNullOrWhiteSpace(_sessionToken))
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

        var request = new SelectCharacterRequest(_sessionToken, characterId);
        _authConnection.Send(new Envelope(OpCode.AuthSelectCharacterRequest, request));

        UpdateStatus($"Selecting character #{characterId}...");
        GD.Print($"[Menu] Character selection request sent: {characterId}");
    }

    private void OnCreateCharacterButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.CharacterCreation))
            return;

        UpdateStatus("Character creation not supported by this server");
    }

    // ✅ ATUALIZADO: Handler de deleção de personagem
    private void OnDeleteCharacterButtonPressed()
    {
        if (!CanTransitionFrom(MenuState.CharacterSelection))
            return;

        UpdateStatus("Character deletion not supported by this server");
    }

    // ✅ NOVO: Confirmação de deleção
    private void OnDeleteConfirmed()
    {
        UpdateStatus("Character deletion not supported by this server");
    }

    // ✅ NOVO: Cancelamento de deleção
    private void OnDeleteCanceled()
    {
        GD.Print("[Menu] Character deletion cancelled by user");
        UpdateStatus("Character deletion cancelled");

        // ✅ Volta para seleção de personagem
        TransitionToState(MenuState.CharacterSelection);
    }

    private void OnExitButtonPressed()
    {
        GD.Print("[Menu] Exiting game...");
        GetTree().Quit();
    }

    // ========== NETWORK EVENTS ==========

    private void OnAuthConnected()
    {
        GD.Print("[Menu] Connected to auth server");

        if (_login.AutoLogin && IsInState(MenuState.Login))
        {
            CallDeferred(nameof(PerformAutoLogin));
        }
    }

    private void OnAuthDisconnected(LiteNetLib.DisconnectInfo info)
    {
        GD.PushWarning($"[Menu] Disconnected from auth server: {info.Reason}");
        if (!IsInState(MenuState.TransitioningToGame))
        {
            UpdateStatus("Disconnected from auth server");
            TransitionToState(MenuState.Error);
        }
    }

    private void OnWorldConnected()
    {
        GD.Print("[Menu] Connected to world server");

        var enterTicket = GameStateManager.Instance.EnterTicket;
        if (string.IsNullOrWhiteSpace(enterTicket))
        {
            UpdateStatus("Missing enter ticket");
            TransitionToState(MenuState.Error);
            return;
        }

        var request = new EnterWorldRequest(enterTicket);
        _worldConnection.Send(new Envelope(OpCode.WorldEnterRequest, request));
        TransitionToState(MenuState.WaitingGameData);
    }

    private void OnWorldDisconnected(LiteNetLib.DisconnectInfo info)
    {
        GD.PushWarning($"[Menu] Disconnected from world server: {info.Reason}");
        if (!IsInState(MenuState.TransitioningToGame))
        {
            UpdateStatus("Disconnected from world server");
            TransitionToState(MenuState.Error);
        }
    }

    // ========== ENVELOPE HANDLERS ==========

    private void OnAuthEnvelopeReceived(Envelope envelope)
    {
        switch (envelope.OpCode)
        {
            case OpCode.AuthLoginResponse:
                HandleAuthLoginResponse(envelope.Payload);
                break;
            case OpCode.AuthCharacterListResponse:
                HandleCharacterListResponse(envelope.Payload);
                break;
            case OpCode.AuthSelectCharacterResponse:
                HandleSelectCharacterResponse(envelope.Payload);
                break;
        }
    }

    private void OnWorldEnvelopeReceived(Envelope envelope)
    {
        switch (envelope.OpCode)
        {
            case OpCode.WorldEnterResponse:
                HandleEnterWorldResponse(envelope.Payload);
                break;
        }
    }

    private void HandleAuthLoginResponse(object? payload)
    {
        if (payload is not AuthLoginResponse response)
        {
            UpdateStatus("Invalid login response");
            return;
        }

        if (!response.Success || string.IsNullOrWhiteSpace(response.Token))
        {
            UpdateStatus($"Login failed: {response.Error}");
            return;
        }

        _sessionToken = response.Token;
        GameStateManager.Instance.AuthToken = response.Token;

        var listRequest = new CharacterListRequest(response.Token);
        _authConnection.Send(new Envelope(OpCode.AuthCharacterListRequest, listRequest));
    }

    private void HandleCharacterListResponse(object? payload)
    {
        if (payload is not CharacterListResponse response)
        {
            UpdateStatus("Invalid character list response");
            return;
        }

        if (!response.Success)
        {
            UpdateStatus($"Character list failed: {response.Error}");
            return;
        }

        _availableCharacters.Clear();
        _availableCharacters.AddRange(response.Characters);

        GD.Print($"[Menu] Character list received: {_availableCharacters.Count}");
        TransitionToState(MenuState.CharacterSelection);

        if (_characterSelection.CharacterId > 0)
        {
            CallDeferred(nameof(PerformAutoCharacterSelection));
        }
    }

    private void HandleSelectCharacterResponse(object? payload)
    {
        if (payload is not SelectCharacterResponse response)
        {
            UpdateStatus("Invalid character selection response");
            return;
        }

        if (!response.Success || string.IsNullOrWhiteSpace(response.WorldEndpoint) || string.IsNullOrWhiteSpace(response.EnterTicket))
        {
            UpdateStatus($"Character selection failed: {response.Error}");
            return;
        }

        var gameState = GameStateManager.Instance;
        gameState.WorldEndpoint = response.WorldEndpoint;
        gameState.EnterTicket = response.EnterTicket;

        if (!TryParseEndpoint(response.WorldEndpoint, out var host, out var port))
        {
            UpdateStatus("Invalid world endpoint");
            TransitionToState(MenuState.Error);
            return;
        }

        TransitionToState(MenuState.Connecting);
        _worldConnection.Connect(host, port);
    }

    private void HandleEnterWorldResponse(object? payload)
    {
        if (payload is not EnterWorldResponse response)
        {
            UpdateStatus("Invalid enter world response");
            TransitionToState(MenuState.Error);
            return;
        }

        if (!response.Success || response.Spawn is null)
        {
            UpdateStatus($"Enter world failed: {response.Error}");
            TransitionToState(MenuState.Error);
            return;
        }

        var spawn = response.Spawn.Value;
        var gameState = GameStateManager.Instance;
        gameState.CharacterId = spawn.CharacterId;
        gameState.CharacterName = spawn.Name;

        GD.Print($"[Menu] Entered world as {spawn.Name} ({spawn.CharacterId})");
        TransitionToState(MenuState.TransitioningToGame);
    }

    // ========== UTILITIES ==========

    private void UpdateCharacterList()
    {
        _characterList.Clear();

        foreach (var character in _availableCharacters)
        {
            var itemText = character.Name;
            _characterList.AddItem(itemText);
        }

        GD.Print($"[Menu] Character list updated - {_availableCharacters.Count} characters");
    }

    private void ResetAuthenticationState()
    {
        _sessionToken = null;
        _availableCharacters.Clear();
        GameStateManager.Instance.ResetState();

        GD.Print("[Menu] Authentication state reset");
    }

    private static bool TryParseEndpoint(string endpoint, out string host, out int port)
    {
        host = string.Empty;
        port = 0;

        if (string.IsNullOrWhiteSpace(endpoint))
            return false;

        var lastColon = endpoint.LastIndexOf(':');
        if (lastColon <= 0 || lastColon == endpoint.Length - 1)
            return false;

        host = endpoint[..lastColon];
        return int.TryParse(endpoint[(lastColon + 1)..], out port);
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
