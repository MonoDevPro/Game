using System.Collections.Generic;
using Game.Contracts;
using Game.Domain;
using Godot;
using GodotClient.Core.Autoloads;
using GodotClient.UI.Actions;
using GodotClient.UI.Chat;
using GodotClient.UI.Joystick;
using GodotClient.Visuals;
using Input = Godot.Input;

namespace GodotClient.Simulation;

/// <summary>
/// Cliente principal do jogo orientado a ECS:
/// - Bootstrapa a ClientSimulation (ECS + sistemas)
/// - Carrega dados iniciais (mapa, ids) do GameStateManager
/// - Registra handlers de rede e encaminha snapshots para a simulação
/// - Roda o loop de simulação (fixed-timestep) em _Process
/// </summary>
public partial class GameClient : Node2D
{
    public static GameClient Instance { get; private set; } = null!;
    public bool IsChatFocused { get; private set; }

    private static readonly string[] GameplayActionsToRelease =
    {
        "walk_west",
        "walk_east",
        "walk_north",
        "walk_south",
        "attack",
        "sprint",
        "click_left",
        "click_right",
    };

    private int _localPlayerId = -1;
    private string _localPlayerName = string.Empty;
    private Label? _statusLabel;
    private ChatHud? _chatHud;
    private VirtualJoystick? _virtualJoystick;
    private ActionHud? _actionHud;
    private CanvasLayer? _hudLayer;
    private NetClientConnection? _worldConnection;
    private NetClientConnection? _chatConnection;
    private WorldSnapshot? _lastSnapshot;
    private readonly Dictionary<int, PlayerVisual> _playersById = new();
    private ulong _lastMoveSentMs;
    private const int MoveSendIntervalMs = 100;
    
    public Node2D EntitiesRoot => GetNode<Node2D>("Map/Entities");

    public override void _Ready()
    {
        base._Ready();
        
        Instance = this;

        CreateHudLayer();

        // 4) Carrega dados iniciais (NetworkId local, snapshot de mapa, etc.)
        LoadGameData();

        RegisterNetworkHandlers();

        GD.Print("[GameClient] Ready (ECS)");
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (_chatHud is not null)
        {
            _chatHud.MessageSubmitted -= OnChatMessageSubmitted;
            _chatHud.InputFocusChanged -= OnChatInputFocusChanged;
            _chatHud = null;
        }

        UnregisterNetworkHandlers();

        _hudLayer = null;

        GD.Print("[GameClient] Unloaded");
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);

        if (_localPlayerId <= 0 || IsChatFocused)
            return;

        var now = Time.GetTicksMsec();
        if (now - _lastMoveSentMs < MoveSendIntervalMs)
            return;

        var dx = 0;
        var dy = 0;
        if (Input.IsActionPressed("walk_west")) dx -= 1;
        if (Input.IsActionPressed("walk_east")) dx += 1;
        if (Input.IsActionPressed("walk_north")) dy -= 1;
        if (Input.IsActionPressed("walk_south")) dy += 1;

        if (dx == 0 && dy == 0)
            return;

        _worldConnection?.Send(new Envelope(OpCode.WorldMoveCommand, new WorldMoveCommand(_localPlayerId, dx, dy)));
        _lastMoveSentMs = now;
    }

    // ==================== Game Data / Map ====================

    private void LoadGameData()
    {
        var gameState = GameStateManager.Instance;
        if (!gameState.Connected)
        {
            GD.PushError("[GameClient] No game data available! Returning to menu...");
            UpdateStatus("Error: No game data");
            SceneManager.Instance.LoadMainMenu();
            return;
        }
        _localPlayerId = gameState.CharacterId;
        _localPlayerName = gameState.CharacterName ?? string.Empty;
        UpdateStatus($"Playing (CharID: {_localPlayerId})");
        GD.Print("[GameClient] Game data loaded");
    }
    
    // ==================== Network Handlers ====================

    private void RegisterNetworkHandlers()
    {
        _worldConnection = NetworkClient.Instance.WorldConnection;
        _chatConnection = NetworkClient.Instance.ChatConnection;

        if (_worldConnection is not null)
            _worldConnection.EnvelopeReceived += OnWorldEnvelopeReceived;

        if (_chatConnection is not null)
            _chatConnection.EnvelopeReceived += OnChatEnvelopeReceived;

        var config = ConfigManager.Instance.Configuration.Network ?? new NetworkConfiguration();
        if (_chatConnection is not null && !_chatConnection.IsConnected)
            _chatConnection.Connect(config.ServerAddress, config.ChatPort);
    }

    private void UnregisterNetworkHandlers()
    {
        if (_worldConnection is not null)
            _worldConnection.EnvelopeReceived -= OnWorldEnvelopeReceived;

        if (_chatConnection is not null)
            _chatConnection.EnvelopeReceived -= OnChatEnvelopeReceived;
    }

    private void OnWorldEnvelopeReceived(Envelope envelope)
    {
        switch (envelope.OpCode)
        {
            case OpCode.WorldSnapshot:
                if (envelope.Payload is WorldSnapshot snapshot)
                {
                    _lastSnapshot = snapshot;
                    ApplySnapshot(snapshot);
                }
                break;
            case OpCode.WorldSnapshotDelta:
                if (envelope.Payload is WorldSnapshotDelta delta)
                {
                    ApplySnapshotDelta(delta);
                }
                break;
        }
    }

    private void OnChatEnvelopeReceived(Envelope envelope)
    {
        if (envelope.OpCode != OpCode.ChatMessage)
            return;

        if (envelope.Payload is not ChatMessage message)
            return;

        _chatHud?.AppendMessage(message);
    }

    private void ApplySnapshotDelta(WorldSnapshotDelta delta)
    {
        if (_lastSnapshot is null)
        {
            RequestFullSnapshot();
            return;
        }

        if (_lastSnapshot.Value.ServerTick != delta.BaseTick)
        {
            RequestFullSnapshot();
            return;
        }

        var updated = SnapshotDeltaCalculator.ApplyDelta(_lastSnapshot.Value, delta);
        _lastSnapshot = updated;
        ApplySnapshot(updated);
    }

    private void ApplySnapshot(WorldSnapshot snapshot)
    {
        var alive = new HashSet<int>();
        foreach (var player in snapshot.Players)
        {
            alive.Add(player.CharacterId);
            var visual = GetOrCreatePlayerVisual(player);
            UpdateVisual(visual, player);
        }

        var toRemove = new List<int>();
        foreach (var id in _playersById.Keys)
        {
            if (!alive.Contains(id))
                toRemove.Add(id);
        }

        foreach (var id in toRemove)
        {
            if (_playersById.TryGetValue(id, out var visual))
            {
                visual.QueueFree();
                _playersById.Remove(id);
            }
        }
    }

    private PlayerVisual GetOrCreatePlayerVisual(PlayerState player)
    {
        if (_playersById.TryGetValue(player.CharacterId, out var existing))
            return existing;

        var visual = PlayerVisual.Create();
        visual.UpdateName(player.Name);
        EntitiesRoot.AddChild(visual);
        _playersById[player.CharacterId] = visual;
        return visual;
    }

    private void UpdateVisual(PlayerVisual visual, PlayerState player)
    {
        visual.UpdatePosition(new Vector3I(player.X, player.Y, player.Floor));
        var direction = GetDirection(player.DirX, player.DirY);
        visual.UpdateAnimationState(direction, player.IsMoving);
    }

    private static Direction GetDirection(int dirX, int dirY)
    {
        return (dirX, dirY) switch
        {
            (0, -1) => Direction.North,
            (0, 1) => Direction.South,
            (1, 0) => Direction.East,
            (-1, 0) => Direction.West,
            (1, -1) => Direction.NorthEast,
            (-1, -1) => Direction.NorthWest,
            (1, 1) => Direction.SouthEast,
            (-1, 1) => Direction.SouthWest,
            _ => Direction.South
        };
    }

    private void RequestFullSnapshot()
    {
        _worldConnection?.Send(new Envelope(OpCode.WorldSnapshotRequest, null));
    }

    private void OnChatMessageSubmitted(string message)
    {
        SendChatMessage(message);
    }

    private void OnChatInputFocusChanged(bool hasFocus)
    {
        IsChatFocused = hasFocus;
        if (!hasFocus)
            return;

        foreach (var action in GameplayActionsToRelease)
        {
            if (string.IsNullOrEmpty(action))
                continue;
            Input.ActionRelease(action);
        }
    }

    private void SendChatMessage(string message)
    {
        var trimmed = message.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        if (_chatConnection is null || !_chatConnection.IsConnected)
            return;

        var sender = string.IsNullOrWhiteSpace(_localPlayerName) ? "Player" : _localPlayerName;
        var chatMessage = new ChatSendRequest("Global", sender, trimmed);
        _chatConnection.Send(new Envelope(OpCode.ChatSendRequest, chatMessage));
    }
    

    // ==================== UI / Disconnect ====================

    public void DisconnectAndReturnToMenu()
    {
        GD.Print("[GameClient] Disconnecting...");

        UpdateStatus("Disconnecting...");

        // Reseta estado global
        GameStateManager.Instance.ResetState();

        // Volta ao menu
        SceneManager.Instance.LoadMainMenu();
    }

    private void OnPeerDisconnected()
    {
        GD.Print("[GameClient] Disconnected from server");
        UpdateStatus("Disconnected from server");
        DisconnectAndReturnToMenu();
    }

    private void CreateHudLayer()
    {
        _hudLayer = new CanvasLayer { Name = "HudLayer", Layer = 100 };

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Position = new Vector2(12, 12),
            Text = "Loading game..."
        };
        _statusLabel.AddThemeColorOverride("font_color", Colors.White);

        _chatHud = ChatHud.CreateInstance();
        _chatHud.MessageSubmitted += OnChatMessageSubmitted;
        _chatHud.InputFocusChanged += OnChatInputFocusChanged;
        
        _virtualJoystick = VirtualJoystick.CreateInstance();
        
        _actionHud = ActionHud.CreateInstance();

        _hudLayer.AddChild(_statusLabel);
        _hudLayer.AddChild(_chatHud);
        _hudLayer.AddChild(_virtualJoystick);
        _hudLayer.AddChild(_actionHud);
        AddChild(_hudLayer);
    }

    private void UpdateStatus(string message)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = message;
        
        GD.Print($"[GameClient] {message}");
    }
}
