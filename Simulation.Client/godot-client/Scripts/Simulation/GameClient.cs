using System;
using System.Linq;
using Game.Core.Extensions;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Snapshots;
using Game.ECS.Services.Map;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;
using Godot;
using GodotClient.Core.Autoloads;
using GodotClient.ECS;
using GodotClient.UI.Actions;
using GodotClient.UI.Chat;
using GodotClient.UI.Joystick;
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
    public ClientGameSimulation Simulation => _simulation ?? throw new InvalidOperationException("Simulation not initialized");
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

    private INetworkManager? _network;
    private ClientGameSimulation? _simulation;

    private int _localNetworkId = -1;
    private int _localPlayerId = -1;
    private string _localPlayerName = string.Empty;
    private Label? _statusLabel;
    private ChatHud? _chatHud;
    private VirtualJoystick? _virtualJoystick;
    private ActionHud? _actionHud;
    private CanvasLayer? _hudLayer;
    
    public Node2D EntitiesRoot => GetNode<Node2D>("Map/Entities");

    public override void _Ready()
    {
        base._Ready();
        
        Instance = this;

        CreateHudLayer();

        // 1) Network vinda do Autoload (menu já inicializou)
        _network = NetworkClient.Instance.NetworkManager;

        // 2) Boot de simulação (fornece INetworkManager via DI para os sistemas que precisarem)
        _simulation = new ClientGameSimulation(_network);

        // 3) Registra handlers de rede apontando para a simulação
        RegisterPacketHandlers();

        // 4) Carrega dados iniciais (NetworkId local, snapshot de mapa, etc.)
        LoadGameData();

        GD.Print("[GameClient] Ready (ECS)");
    }

    public override void _Process(double delta)
    {
        // Avança a simulação (fixed timestep interno)
        _simulation?.Update((float)delta);
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (_network is not null)
        {
            _network.OnPeerDisconnected -= OnPeerDisconnected;
            
            _network.UnregisterPacketHandler<PlayerSpawnPacket>();
            _network.UnregisterPacketHandler<LeftPacket>();
            _network.UnregisterPacketHandler<StatePacket>();
            _network.UnregisterPacketHandler<VitalsPacket>();
            _network.UnregisterPacketHandler<AttackPacket>();
            _network.UnregisterPacketHandler<NpcSpawnPacket>();
            _network.UnregisterPacketHandler<ChatMessagePacket>();
        }

        if (_chatHud is not null)
        {
            _chatHud.MessageSubmitted -= OnChatMessageSubmitted;
            _chatHud.InputFocusChanged -= OnChatInputFocusChanged;
            _chatHud = null;
        }

        _hudLayer = null;

        GD.Print("[GameClient] Unloaded");
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

        _localNetworkId = gameState.LocalNetworkId;
        if (gameState.CurrentGameData is { } joinPacket)
        {
            _localPlayerId = joinPacket.LocalPlayer.PlayerId;
            _localPlayerName = joinPacket.LocalPlayer.Name;
        }

        // Mapa (predição clientside e navegação opcional)
        var mapSnap = gameState.CurrentGameData?.MapDataPacket;
        if (mapSnap is null)
        {
            GD.PushError("[GameClient] Map data is null! Returning to menu...");
            UpdateStatus("Error: Map data is null");
            SceneManager.Instance.LoadMainMenu();
            return;
        }

        var width = mapSnap.Value.Width;
        var height = mapSnap.Value.Height;
        var layers = mapSnap.Value.Layers;
        bool[,,] collisionMasks = new bool[width, height, layers];
        
        for (var z = 0; z < layers; z++)
        {
            var collisionLayer = mapSnap.Value.LoadCollisionLayer(z);
            for (var x = 0; x < width; x++)
            for (var y = 0; y < height; y++)
                collisionMasks[x, y, z] = collisionLayer[y * width + x] != 0;
        }
        // Registra logs dos mapas
        GD.Print($"[GameClient] Loaded map '{mapSnap.Value.MapId}' ({width}x{height}x{layers})");
        GD.Print($"[GameClient] Collision cells: {collisionMasks.Cast<bool>().Count(b => b)}/{width * height * layers}");
        
        var mapGrid = new MapGrid(width, height, layers, collisionMasks);
        var spatial = new MapSpatial();
        
        _simulation?.RegisterMap(mapSnap.Value.MapId, mapGrid, spatial);
        UpdateStatus($"Playing (NetID: {_localNetworkId})");
        
        // Carrega visuais dos jogadores
        LoadPlayerVisuals();
        LoadNpcVisuals();

        GD.Print("[GameClient] Game data loaded");
    }
    
    private void LoadPlayerVisuals()
    {
        var gameState = GameStateManager.Instance;
        if (gameState.CurrentGameData is null) return;
        
        var localPlayer = gameState.CurrentGameData.Value.LocalPlayer;

        var localPlayerSnapshot = localPlayer.ToPlayerSnapshot();

        _simulation?.CreateLocalPlayer(
            ref localPlayerSnapshot,
            SpawnPlayerVisual(localPlayer.ToPlayerSnapshot()));
    }

    private void LoadNpcVisuals()
    {
        var bufferedNpcs = GameStateManager.Instance.ConsumeNpcSnapshots();
        
        GD.Print($"[GameClient] Spawning {bufferedNpcs.Length} buffered NPCs");
        
        foreach (var snapshot in bufferedNpcs)
        {
            var npcData = snapshot.ToNpcSnapshot();
            _simulation?.CreateNpc(ref npcData, SpawnNpcVisual(npcData));
        }
    }
    
    private PlayerVisual SpawnPlayerVisual(in PlayerSnapshot snapshot)
    {
        var playerVisual = PlayerVisual.Create();
        
        playerVisual.Name = $"Player_{snapshot.NetworkId}";
        
        return playerVisual;
    }

    // ==================== Network Handlers ====================

    private void RegisterPacketHandlers()
    {
        if (_network is null)
        {
            GD.PushError("[GameClient] NetworkManager is null!");
            return;
        }

        _network.OnPeerDisconnected += OnPeerDisconnected;
        
        _network.RegisterPacketHandler<PlayerSpawnPacket>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<LeftPacket>(HandleDespawn);
        _network.RegisterPacketHandler<StatePacket>(HandleState);
        _network.RegisterPacketHandler<VitalsPacket>(HandleVitals);
        _network.RegisterPacketHandler<AttackPacket>(HandleCombatState);
        _network.RegisterPacketHandler<NpcSpawnPacket>(HandleNpcSpawn);
        _network.RegisterPacketHandler<ChatMessagePacket>(HandleChatMessage);

        GD.Print("[GameClient] Packet handlers registered (ECS)");
    }

    private void HandleState(INetPeerAdapter peer, ref StatePacket packet)
    {
        if (_simulation is null)
            return;
        
        foreach (var singlePacket in packet.States)
        {
            var stateSnapshot = singlePacket.ToStateSnapshot();
            _simulation?.ApplyState(ref stateSnapshot);
        }
    }

    private void HandleVitals(INetPeerAdapter peer, ref VitalsPacket packet)
    {
        if (_simulation is null)
            return;
        
        foreach (var singlePacket in packet.Vitals)
            HandleSingleVitals(singlePacket);
    }
    
    private void HandleSingleVitals(in VitalsData packet)
    {
        if (_simulation is null)
            return;
        
        if (!_simulation.TryGetAnyEntity(packet.NetworkId, out var entity))
            return;
        if (!_simulation.TryGetAnyVisual(packet.NetworkId, out var visual))
            return;
        
        var heathCurrent = _simulation.World.Get<Health>(entity).Current;
        if (packet.CurrentHp < heathCurrent)
        {
            var damageAmount = heathCurrent - packet.CurrentHp;
            // Exemplo simples: troca cor rápida
            visual.ChangeTempColor(Colors.Red, 0.2f);
            visual.CreateFloatingDamageLabel(damageAmount, critical: false);
        }
        if (packet.CurrentHp > heathCurrent)
        {
            var healAmount = packet.CurrentHp - heathCurrent;
            visual.CreateFloatingHealLabel(healAmount);
        }
        
        if (packet.CurrentHp <= 0 && !_simulation.World.Has<Dead>(entity))
            _simulation.World.Add<Dead>(entity);
        else if (_simulation.World.Has<Dead>(entity))
            _simulation.World.Remove<Dead>(entity);
        
        var vitalsSnapshot = packet.ToVitalsSnapshot();
        _simulation.ApplyVitals(ref vitalsSnapshot);
        
        visual.UpdateVitals(packet.CurrentHp, packet.MaxHp, packet.CurrentMp, packet.MaxMp);
        
        GD.Print($"[GameClient] Received PlayerVitalsPacket for NetworkId {packet.NetworkId}");
    }
    
    private void HandlePlayerSpawn(INetPeerAdapter peer, ref PlayerSpawnPacket packet)
    {
        foreach (var singlePacket in packet.PlayerData)
        {
            PlayerData dataPacket = singlePacket;
            HandlePlayerSpawn(peer, ref dataPacket);
        }
    }
    private void HandlePlayerSpawn(INetPeerAdapter peer, ref PlayerData dataPacket)
    {
        var isLocal = dataPacket.NetworkId == _localNetworkId;
        var playerData = dataPacket.ToPlayerSnapshot();

        if (isLocal)
            _simulation?.CreateLocalPlayer(ref playerData, SpawnPlayerVisual(playerData));
        else
            _simulation?.CreateRemotePlayer(ref playerData, SpawnPlayerVisual(playerData));
        
        if (!isLocal) 
            UpdateStatus($"{playerData.Name} joined");
        
        GD.Print($"[GameClient] Spawned player '{dataPacket.Name}' (NetID: {dataPacket.NetworkId})");
    }
    
    private void HandleDespawn(INetPeerAdapter peer, ref LeftPacket packet)
    {
        foreach (var networkId in packet.NetworkIds)
        {
            _simulation?.DestroyAny(networkId);
            GD.Print($"[GameClient] Despawned player (NetID: {networkId})");
            
            if (networkId == _localNetworkId)
            {
                GD.PushWarning("[GameClient] You have been disconnected from the server!");
                UpdateStatus("You have been disconnected from the server");
                DisconnectAndReturnToMenu();
            }
        }
    }
    
    private void HandleCombatState(INetPeerAdapter peer, ref AttackPacket packet)
    {
        foreach (var singlePacket in packet.Attacks)
            HandleSingleCombatState(singlePacket);
    }
    
    private void HandleSingleCombatState(in AttackData packet)
    {
        GD.Print($"[GameClient] HandleCombatState called: Attacker={packet.AttackerNetworkId}");
        
        if (_simulation is null)
        {
            GD.PushWarning("[GameClient] HandleCombatState: simulation is null!");
            return;
        }

        // Localiza entidade do atacante
        if (!_simulation.TryGetAnyEntity(packet.AttackerNetworkId, out var attackerEntity))
        {
            GD.PushWarning($"[GameClient] HandleCombatState: Could not find attacker entity {packet.AttackerNetworkId}");
            return;
        }
        
        ref var command = ref _simulation.World.AddOrGet<AttackCommand>(attackerEntity);
        command.Style = packet.Style;
        command.ConjureDuration = packet.AttackDuration;

        ref var state = ref _simulation.World.AddOrGet<CombatState>(attackerEntity);
        state.AttackCooldownTimer = packet.CooldownRemaining;

        GD.Print($"[GameClient] CombatStatePacket processed: Attacker={packet.AttackerNetworkId}, Type={packet.Style}, Duration={packet.AttackDuration}, Cooldown={packet.CooldownRemaining}");
    }

    private void HandleNpcSpawn(INetPeerAdapter peer, ref NpcSpawnPacket packet)
    {
        if (_simulation is null)
            return;
        
        GD.Print($"[GameClient] Handling NpcSpawnPacket with {packet.Npcs.Length} NPCs");

        foreach (var npc in packet.Npcs)
        {
            var npcData = npc.ToNpcSnapshot();
            _simulation.CreateNpc(ref npcData, SpawnNpcVisual(npcData));
        }
    }

    private NpcVisual SpawnNpcVisual(NpcSnapshot npcSnapshot)
    {
        var npcVisual = NpcVisual.Create();
        npcVisual.Name = $"Npc_{npcSnapshot.NetworkId}";
        return npcVisual;
    }
    
    private void HandleChatMessage(INetPeerAdapter peer, ref ChatMessagePacket packet)
    {
        _chatHud?.AppendMessage(packet);
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
        if (_network is null || !_network.IsRunning)
            return;

        var trimmed = message.Trim();
        if (string.IsNullOrEmpty(trimmed))
            return;

        var packet = new ChatMessagePacket(
            _localPlayerId,
            _localNetworkId,
            string.IsNullOrWhiteSpace(_localPlayerName) ? "Unknown" : _localPlayerName,
            trimmed,
            DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            false,
            false);

        _network.SendToServer(packet, NetworkChannel.Chat, NetworkDeliveryMethod.ReliableOrdered);
    }
    

    // ==================== UI / Disconnect ====================

    public void DisconnectAndReturnToMenu()
    {
        GD.Print("[GameClient] Disconnecting...");

        UpdateStatus("Disconnecting...");

        // Para rede
        if (_network?.IsRunning == true)
            _network.Stop();

        _simulation?.Dispose();

        // Reseta estado global
        GameStateManager.Instance.ResetState();

        // Volta ao menu
        SceneManager.Instance.LoadMainMenu();
    }

    private void OnPeerDisconnected(INetPeerAdapter peer)
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