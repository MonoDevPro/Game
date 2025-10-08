using System;
using System.Collections.Generic;
using Game.Abstractions.Network;
using Game.Domain.Enums;
using Game.Network.Packets;
using Godot;

namespace GodotClient;

public partial class GameClient : Node
{
    private ApiClient? _apiClient;
    private ConfigManager? _configManager;
    private PlayerView? _playerView;
    private GodotInputSystem? _inputSystem;
    private INetworkManager? _network;
    private LoginConfiguration _login = new();
    private RegistrationConfiguration _registration = new();
    private Label? _statusLabel;
    private bool _registrationAttempted;
    private bool _loginAttempted;

    private readonly Dictionary<int, PlayerSnapshot> _players = new();
    private bool _isAuthenticated;
    private uint _inputSequence;
    private int _localNetworkId = -1;

    public bool CanSendInput => _isAuthenticated && _network is not null;

    public override void _Ready()
    {
        base._Ready();

        _apiClient = GetNode<ApiClient>("%ApiClient");
        _configManager = GetNode<ConfigManager>("%ConfigManager");
        _playerView = GetNode<PlayerView>("PlayerView");
        _inputSystem = GetNode<GodotInputSystem>("GodotInputSystem");

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
        _network.RegisterPacketHandler<PlayerSpawnPacket>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<PlayerStatePacket>(HandlePlayerState);
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
            _network.UnregisterPacketHandler<PlayerSpawnPacket>();
            _network.UnregisterPacketHandler<PlayerStatePacket>();
            _network.UnregisterPacketHandler<PlayerDespawnPacket>();
        }

        _inputSystem?.Detach();
    }

    public void QueueInput(sbyte moveX, sbyte moveY, ushort buttons)
    {
        if (!CanSendInput || _network is null)
        {
            return;
        }

        var packet = new PlayerInputPacket(++_inputSequence, moveX, moveY, buttons);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.Sequenced);
    }

    private void OnPeerConnected(INetPeerAdapter peer)
    {
        UpdateStatus("Connected to server. Preparing sessão...");

        if (_registration.AutoRegister && !_registrationAttempted)
        {
            TrySendRegistration();
            return;
        }

        if (_login.AutoLogin)
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
        {
            return;
        }

        _registrationAttempted = true;

        var username = _login.Username;
        var password = _login.Password;
        var email = _registration.Email;
        var characterName = string.IsNullOrWhiteSpace(_registration.CharacterName) ? username : _registration.CharacterName;

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

        if (string.IsNullOrWhiteSpace(characterName))
        {
            characterName = username;
        }

        if (string.IsNullOrWhiteSpace(_login.CharacterName))
        {
            _login.CharacterName = characterName;
        }

        var packet = new RegistrationRequestPacket(username, email, password, characterName, _registration.Gender, _registration.Vocation);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus("Enviando registro...");
    }

    private void TrySendLogin()
    {
        if (_network is null || _loginAttempted) return;

        if (string.IsNullOrWhiteSpace(_login.Username) || string.IsNullOrWhiteSpace(_login.Password))
        {
            GD.PushWarning("Login configuration is missing username or password. Update appsettings.json.");
            return;
        }

        var packet = new LoginRequestPacket(_login.Username, _login.Password, _login.CharacterName);
        _network.SendToServer(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        UpdateStatus("Authenticating...");
        _loginAttempted = true;
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

        UpdateStatus($"Logged in as {packet.LocalPlayer.Name}");

        _isAuthenticated = true;
        _localNetworkId = packet.LocalPlayer.NetworkId;
        _players[_localNetworkId] = packet.LocalPlayer;
        _inputSequence = 0;

        _playerView?.SetLocalPlayer(packet.LocalPlayer);
        _playerView?.ApplySnapshot(packet.LocalPlayer, true);

        foreach (var snapshot in packet.OnlinePlayers)
            if (_players.TryAdd(snapshot.NetworkId, snapshot))
                _playerView?.ApplySnapshot(snapshot, false);
    }

    private void HandleRegistrationResponse(INetPeerAdapter peer, RegistrationResponsePacket packet)
    {
        if (packet.Success)
        {
            UpdateStatus("Conta criada com sucesso! Aguardando confirmação de login...");
            return;
        }

        GD.PushError($"Registro falhou: {packet.Message}");
        UpdateStatus($"Registro falhou: {packet.Message}");

        if (_login.AutoLogin && !_loginAttempted)
            TrySendLogin();
    }

    private void HandlePlayerSpawn(INetPeerAdapter peer, PlayerSpawnPacket packet)
    {
        if (_players.TryAdd(packet.Player.NetworkId, packet.Player))
        {
            var isLocal = packet.Player.NetworkId == _localNetworkId;
            _playerView?.ApplySnapshot(packet.Player, isLocal);
        }
    }

    private void HandlePlayerState(INetPeerAdapter peer, PlayerStatePacket packet)
    {
        if (_players.TryGetValue(packet.NetworkId, out var snapshot))
        {
            snapshot = new PlayerSnapshot(packet.NetworkId, snapshot.PlayerId, snapshot.CharacterId, snapshot.Name, packet.Position, packet.Facing);
            _players[packet.NetworkId] = snapshot;
            _playerView?.UpdateState(packet);
        }
    }

    private void HandlePlayerDespawn(INetPeerAdapter peer, PlayerDespawnPacket packet)
    {
        if (_players.Remove(packet.NetworkId))
        {
            _playerView?.RemovePlayer(packet.NetworkId);
        }
    }

    private void ResetState()
    {
        _isAuthenticated = false;
        _inputSequence = 0;
        _localNetworkId = -1;
        _registrationAttempted = false;
        _loginAttempted = false;
        _players.Clear();
        _playerView?.Clear();
    }

    private void UpdateStatus(string message)
    {
        if (_statusLabel is not null)
        {
            _statusLabel.Text = message;
        }
        GD.Print(message);
    }
}