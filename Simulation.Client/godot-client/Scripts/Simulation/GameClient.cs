using System;
using System.Linq;
using Game.Core.Extensions;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Services;
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
            
            _network.UnregisterPacketHandler<PlayerSnapshot>();
            _network.UnregisterPacketHandler<LeftPacket>();
            _network.UnregisterPacketHandler<PlayerStatePacket>();
            _network.UnregisterPacketHandler<PlayerVitalsPacket>();
            _network.UnregisterPacketHandler<CombatStatePacket>();
            _network.UnregisterPacketHandler<NpcSpawnPacket>();
            _network.UnregisterPacketHandler<NpcDespawnPacket>();
            _network.UnregisterPacketHandler<NpcStatePacket>();
            _network.UnregisterPacketHandler<NpcHealthPacket>();
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
        var remotePlayers = gameState.CurrentGameData.Value.OtherPlayers;
        SpawnPlayerVisual(localPlayer.ToPlayerData(), true);
        
        foreach (var data in remotePlayers)
            SpawnPlayerVisual(data.ToPlayerData(), false);
    }

    private void LoadNpcVisuals()
    {
        var bufferedNpcs = GameStateManager.Instance.ConsumeNpcSnapshots();
        if (bufferedNpcs.Length == 0)
        {
            GD.Print("[GameClient] No buffered NPC snapshots to spawn during load");
            return;
        }

        GD.Print($"[GameClient] Spawning {bufferedNpcs.Length} buffered NPCs");
        foreach (var snapshot in bufferedNpcs)
            SpawnNpcVisual(snapshot);
    }
    
    private void SpawnPlayerVisual(in PlayerData data, bool isLocal)
    {
        if (_simulation is null) return;
        GD.Print($"[GameClient] Spawning player visual for '{data.Name}' (NetID: {data.NetworkId}, Local: {isLocal})");
        
        var playerVisual = PlayerVisual.Create();
        playerVisual.Name = $"Player_{data.NetworkId}";

        if (isLocal)
        {
            _simulation.SpawnLocalPlayer(data, playerVisual);
            playerVisual.MakeCamera();
            return;
        }
        _simulation.SpawnRemotePlayer(data, playerVisual);
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
        
        _network.RegisterPacketHandler<PlayerSnapshot>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<LeftPacket>(HandlePlayerDespawn);
        _network.RegisterPacketHandler<PlayerStatePacket>(HandlePlayerState);
        _network.RegisterPacketHandler<PlayerVitalsPacket>(HandlePlayerVitals);
        _network.RegisterPacketHandler<CombatStatePacket>(HandleCombatState);
        _network.RegisterPacketHandler<NpcSpawnPacket>(HandleNpcSpawn);
        _network.RegisterPacketHandler<NpcDespawnPacket>(HandleNpcDespawn);
        _network.RegisterPacketHandler<NpcStatePacket>(HandleNpcState);
        _network.RegisterPacketHandler<NpcHealthPacket>(HandleNpcVitals);
        _network.RegisterPacketHandler<ChatMessagePacket>(HandleChatMessage);

        GD.Print("[GameClient] Packet handlers registered (ECS)");
    }

    private void HandlePlayerState(INetPeerAdapter peer, ref PlayerStatePacket packet)
    {
        foreach (var singlePacket in packet.States)
            HandleSinglePlayerState(singlePacket);
    }
    
    private void HandleSinglePlayerState(in PlayerStateSnapshot packet)
    {
        if (_simulation is null)
            return;
        
        // Remotos: aplica tudo
        if (packet.NetworkId != _localNetworkId)
        {
            _simulation?.ApplyPlayerState(packet.ToPlayerStateData());
            return;
        }

        // Local: reconciliação inteligente
        if (_simulation is null || !_simulation.TryGetPlayerEntity(packet.NetworkId, out var entity))
            return;

        ref var localPos = ref _simulation.World.Get<Position>(entity);
        int deltaX = Math.Abs(localPos.X - packet.PositionX);
        int deltaY = Math.Abs(localPos.Y - packet.PositionY);
        
        const int threshold = 1;
        if (deltaX >= threshold || deltaY >= threshold)
        {
            // Grande divergência → servidor manda
            _simulation.World.Get<Movement>(entity).Timer = 0f; // Reset timer
            _simulation.ApplyPlayerState(packet.ToPlayerStateData());
        }
        else
        {
            // Pequena divergência → confia no cliente, só atualiza velocity/facing
            _simulation.World.Get<Velocity>(entity) = new Velocity
            {
                DirectionX = packet.VelocityX,
                DirectionY = packet.VelocityY,
                Speed = packet.Speed
            };
            _simulation.World.Get<Facing>(entity) = new Facing
            {
                DirectionX = packet.FacingX,
                DirectionY = packet.FacingY
            };
        }
    }

    private void HandlePlayerVitals(INetPeerAdapter peer, ref PlayerVitalsPacket packet)
    {
        if (_simulation is null)
            return;
        
        foreach (var singlePacket in packet.Vitals)
            HandleSinglePlayerVitals(singlePacket);
    }
    
    private void HandleSinglePlayerVitals(in PlayerVitalsSnapshot packet)
    {
        
        if (_simulation is null)
            return;
        
        // Localiza entidade do atacante
        if (!_simulation.TryGetPlayerEntity(packet.NetworkId, out var playerEntity))
            return;
        
        if (!_simulation.TryGetPlayerVisual(packet.NetworkId, out var playerVisual))
            return;
        
        var heathCurrent = _simulation.World.Get<Health>(playerEntity).Current;
        if (packet.CurrentHp < heathCurrent)
        {
            var damageAmount = heathCurrent - packet.CurrentHp;
            // Exemplo simples: troca cor rápida
            playerVisual.ChangeTempColor(Colors.Red, 0.2f);
            playerVisual.CreateFloatingDamageLabel(damageAmount, critical: false);
        }
        if (packet.CurrentHp > heathCurrent)
        {
            var healAmount = packet.CurrentHp - heathCurrent;
            playerVisual.CreateFloatingHealLabel(healAmount);
        }
        
        if (packet.CurrentHp <= 0 && !_simulation.World.Has<Dead>(playerEntity))
            _simulation.World.Add<Dead>(playerEntity);
        else if (_simulation.World.Has<Dead>(playerEntity))
            _simulation.World.Remove<Dead>(playerEntity);
        
        _simulation.ApplyPlayerVitals(packet.ToPlayerVitalsData());
        
        playerVisual.UpdateVitals(packet.CurrentHp, packet.MaxHp, packet.CurrentMp, packet.MaxMp);
        
        GD.Print($"[GameClient] Received PlayerVitalsPacket for NetworkId {packet.NetworkId}");
    }

    private void HandlePlayerSpawn(INetPeerAdapter peer, ref PlayerSnapshot snapshot)
    {
        if (_simulation is null) return;
        var isLocal = snapshot.NetworkId == _localNetworkId;
        var playerData = snapshot.ToPlayerData();
        SpawnPlayerVisual(playerData, isLocal);
        UpdateStatus($"{snapshot.Name} joined");
        
        GD.Print($"[GameClient] Spawned player '{snapshot.Name}' (NetID: {snapshot.NetworkId})");
    }
    
    private void HandlePlayerDespawn(INetPeerAdapter peer, ref LeftPacket packet)
    {
        _simulation?.DespawnPlayer(packet.NetworkId);
        if (packet.NetworkId == _localNetworkId)
        {
            GD.PushWarning("[GameClient] You have been disconnected from the server!");
            UpdateStatus("You have been disconnected from the server");
            DisconnectAndReturnToMenu();
        }
        
        GD.Print($"[GameClient] Despawned player (NetID: {packet.NetworkId})");
    }
    
    private void HandleCombatState(INetPeerAdapter peer, ref CombatStatePacket packet)
    {
        foreach (var singlePacket in packet.CombatStates)
            HandleSingleCombatState(singlePacket);
    }
    private void HandleSingleCombatState(in CombatStateSnapshot packet)
    {
        GD.Print($"[GameClient] HandleCombatState called: Attacker={packet.AttackerNetworkId}");
        
        if (_simulation is null)
        {
            GD.PushWarning("[GameClient] HandleCombatState: simulation is null!");
            return;
        }

        // Localiza entidade do atacante
        if (!_simulation.TryGetPlayerEntity(packet.AttackerNetworkId, out var attackerEntity))
        {
            GD.PushWarning($"[GameClient] HandleCombatState: Could not find attacker entity {packet.AttackerNetworkId}");
            return;
        }

        // Se quiser armazenar algo temporário no ECS local para animação:
        var attackAnim = new Attack
        {
            Type = packet.Type,
            RemainingDuration = packet.AttackDuration,
            TotalDuration = packet.AttackDuration,
            DamageApplied = false
        };
        _simulation.World.Add(attackerEntity, attackAnim);

        GD.Print($"[GameClient] CombatStatePacket processed: Attacker={packet.AttackerNetworkId}, Type={packet.Type}, Duration={packet.AttackDuration}, Cooldown={packet.CooldownRemaining}");
    }

    private void HandleNpcSpawn(INetPeerAdapter peer, ref NpcSpawnPacket packet)
    {
        if (_simulation is null || packet.Npcs.Length == 0)
            return;
        
        GD.Print($"[GameClient] Handling NpcSpawnPacket with {packet.Npcs.Length} NPCs");

        foreach (var npc in packet.Npcs)
            SpawnNpcVisual(npc);
    }

    private void HandleNpcDespawn(INetPeerAdapter peer, ref NpcDespawnPacket packet)
    {
        if (_simulation is null)
            return;
        
        GD.Print($"[GameClient] Handling NpcDespawnPacket for NetworkId {packet.NetworkId}");
        
        _simulation.DespawnNpc(packet.NetworkId);
    }

    private void HandleNpcState(INetPeerAdapter peer, ref NpcStatePacket packet)
    {
        if (_simulation is null || packet.States.Length == 0)
            return;
        
        GD.Print($"[GameClient] Handling NpcStatePacket with {packet.States.Length} NPC states");

        foreach (var state in packet.States)
            _simulation.ApplyNpcState(state.ToNpcStateData());
    }
    
    private void HandleNpcVitals(INetPeerAdapter peer, ref NpcHealthPacket packet)
    {
        if (_simulation is null || packet.Healths.Length == 0)
            return;
        
        GD.Print($"[GameClient] Handling NpcHealthPacket with {packet.Healths.Length} NPC vitals");

        foreach (var vitals in packet.Healths)
            _simulation.ApplyNpcVitals(vitals.ToNpcHealthData());
    }

    private void SpawnNpcVisual(NpcSpawnSnapshot spawnSnapshot)
    {
        if (_simulation is null)
            return;

        var npcData = spawnSnapshot.ToNpcData();

        if (_simulation.TryGetNpcEntity(npcData.NetworkId, out _))
            _simulation.DespawnNpc(npcData.NetworkId);

        var npcVisual = NpcVisual.Create();
        npcVisual.Name = $"Npc_{npcData.NetworkId}";
        _simulation.SpawnNpc(npcData, npcVisual);
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