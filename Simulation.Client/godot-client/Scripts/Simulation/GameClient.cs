using System;
using Game.DTOs.Chat;
using Game.Network.Abstractions;
using GameECS.Client;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Entities.Shared.Data;
using GameECS.Modules.Navigation.Shared.Data;
using Godot;
using GodotClient.Core.Autoloads;
using GodotClient.Simulation.Contracts;
using GodotClient.UI.Actions;
using GodotClient.UI.Chat;
using GodotClient.UI.Joystick;
using Input = Godot.Input;

namespace GodotClient.Simulation;

/// <summary>
/// Cliente principal do jogo orientado a ECS:
/// - Bootstrapa a ClientGameSimulation (ECS + sistemas)
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

        // 2) Boot de simulação
        _simulation = new ClientGameSimulation(
            inputProvider: new GodotInputProvider(),
            networkSender: new NetworkSender(_network));
        
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
            _network.UnregisterPacketHandler<MovementSnapshot>();
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
        var mapSnap = gameState.CurrentGameData?.MapData;
        if (mapSnap is null)
        {
            GD.PushError("[GameClient] Map data is null! Returning to menu...");
            UpdateStatus("Error: Map data is null");
            SceneManager.Instance.LoadMainMenu();
            return;
        }

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
        
        // Visual
        var playerVisual = SpawnPlayerVisual(localPlayer);
        _simulation?.CreateLocalPlayer(localPlayer, playerVisual);
        EntitiesRoot.AddChild(playerVisual);
        playerVisual.MakeCamera();
        
        var bufferedPlayers = gameState.ConsumePlayerSnapshots();

        GD.Print($"[GameClient] Spawning {bufferedPlayers.Length} buffered players");

        foreach (var snapshot in bufferedPlayers)
        {
            var visual = SpawnPlayerVisual(snapshot);
            _simulation?.CreateRemotePlayer(snapshot, visual);
            EntitiesRoot.AddChild(visual);
        }
    }
    
    /*
    bool isMoving = animation.Clip is AnimationClip.Walk or AnimationClip.Run;
    bool isAttacking = animation.Clip == AnimationClip.Attack;
    var isDead = animation.Clip == AnimationClip.Death;
    visual.UpdateAnimationState(animation.Facing, isMoving, isAttacking, isDead);
    */

    private void LoadNpcVisuals()
    {
        var bufferedNpcs = GameStateManager.Instance.ConsumeNpcSnapshots();
        
        GD.Print($"[GameClient] Spawning {bufferedNpcs.Length} buffered NPCs");
        
        foreach (var snapshot in bufferedNpcs)
        {
            // Visual
            var npcVisual = SpawnNpcVisual(snapshot);
            _simulation?.CreateNpc(snapshot, npcVisual);
            EntitiesRoot.AddChild(npcVisual);
        }
    }
    
    private Visuals.PlayerVisual SpawnPlayerVisual(in PlayerData snapshot)
    {
        var playerVisual = Visuals.PlayerVisual.Create();
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
        _network.RegisterPacketHandler<MovementSnapshot>(HandleMovement);
        _network.RegisterPacketHandler<VitalsPacket>(HandleVitals);
        _network.RegisterPacketHandler<AttackPacket>(HandleCombatState);
        _network.RegisterPacketHandler<NpcSpawnPacket>(HandleNpcSpawn);
        _network.RegisterPacketHandler<ChatMessagePacket>(HandleChatMessage);

        GD.Print("[GameClient] Packet handlers registered (ECS)");
    }

    private void HandleMovement(INetPeerAdapter peer, ref MovementSnapshot packet)
    {
        _simulation?.NavigationModule?.OnMovementSnapshot(packet);
    }

    private void HandleVitals(INetPeerAdapter peer, ref VitalsPacket packet)
    {
        if (_simulation is null)
            return;
        
        foreach (var singlePacket in packet.Vitals)
            HandleSingleVitals(singlePacket);
    }

    private void HandleSingleVitals(in VitalsSnapshot packet)
    {
        if (_simulation is null)
            return;

        if (!_simulation.TryGetAnyEntity(packet.NetworkId, out var entity))
        {
            GD.PushWarning($"[GameClient] HandleSingleVitals: Could not find entity {packet.NetworkId}");
            return;
        }

        if (!_simulation.World.TryGet<Health>(entity, out var health))
        {
            GD.PushWarning($"[GameClient] HandleSingleVitals: Entity {packet.NetworkId} has no Health component");
            return;
        }

        var heathCurrent = health.Current;
        if (packet.CurrentHp < heathCurrent)
        {
            var damageAmount = heathCurrent - packet.CurrentHp;
            // TODO: Implementar visual feedback quando visual estiver disponível
            // visual.ChangeTempColor(Colors.Red, 0.2f);
            // visual.CreateFloatingDamageLabel(damageAmount, critical: false);
        }

        if (packet.CurrentHp > heathCurrent)
        {
            var healAmount = packet.CurrentHp - heathCurrent;
            // TODO: Implementar visual feedback quando visual estiver disponível
            // visual.CreateFloatingHealLabel(healAmount);
        }

        if (packet.CurrentHp <= 0)
        {
            if (!_simulation.World.Has<Dead>(entity))
                _simulation.World.Add<Dead>(entity);
        }
        else
        {
            if (_simulation.World.Has<Dead>(entity))
                _simulation.World.Remove<Dead>(entity);
        }

        _simulation.ApplyVitals(packet);
        
        // TODO: Implementar visual feedback quando visual estiver disponível
        // visual.UpdateVitals(packet.CurrentHp, packet.MaxHp, packet.CurrentMp, packet.MaxMp);
        
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
        var visual = SpawnPlayerVisual(dataPacket);

        if (isLocal)
            _simulation?.CreateLocalPlayer(dataPacket, visual);
        else
            _simulation?.CreateRemotePlayer(dataPacket, visual);
        
        EntitiesRoot.AddChild(visual);
        
        if (isLocal)
            visual.MakeCamera();
        
        if (!isLocal) 
            UpdateStatus($"{dataPacket.Name} joined");
        
        GD.Print($"[GameClient] Spawned player '{dataPacket.Name}' (NetID: {dataPacket.NetworkId})");
    }
    
    private void HandleDespawn(INetPeerAdapter peer, ref LeftPacket packet)
    {
        foreach (var networkId in packet.Ids)
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
    
    private void HandleSingleCombatState(in AttackSnapshot packet)
    {
        GD.Print($"[GameClient] HandleCombatState called: Attacker={packet.AttackerId}");
        
        if (_simulation is null)
        {
            GD.PushWarning("[GameClient] HandleCombatState: simulation is null!");
            return;
        }

        // Localiza entidade do atacante
        if (!_simulation.TryGetAnyEntity(packet.AttackerId, out var attackerEntity))
        {
            GD.PushWarning($"[GameClient] HandleCombatState: Could not find attacker entity {packet.AttackerId}");
            return;
        }
        
        // TODO: Implementar AttackCommand e CombatState quando componentes estiverem disponíveis
        // ref var command = ref _simulation.World.AddOrGet<AttackCommand>(attackerEntity);
        // command.Style = packet.Style;
        // command.ConjureDuration = packet.AttackDuration;

        // ref var state = ref _simulation.World.AddOrGet<CombatState>(attackerEntity);
        // state.CooldownTimer = packet.CooldownRemaining;

        GD.Print($"[GameClient] CombatStatePacket processed: Attacker={packet.AttackerId}, Type={packet.Style}, Duration={packet.AttackDuration}, Cooldown={packet.CooldownRemaining}");
    }

    private void HandleNpcSpawn(INetPeerAdapter peer, ref NpcSpawnPacket packet)
    {
        if (_simulation is null)
            return;
        
        GD.Print($"[GameClient] Handling NpcSpawnPacket with {packet.Npcs.Length} NPCs");

        foreach (var npc in packet.Npcs)
        {
            var npcVisual = SpawnNpcVisual(npc);
            _simulation.CreateNpc(npc, SpawnNpcVisual(npc));
            EntitiesRoot.AddChild(npcVisual);
        }
    }

    private Visuals.NpcVisual SpawnNpcVisual(in NpcData npcDataSnapshot)
    {
        var npcVisual = Visuals.NpcVisual.Create();
        npcVisual.Name = $"Npc_{npcDataSnapshot.NetworkId}";
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