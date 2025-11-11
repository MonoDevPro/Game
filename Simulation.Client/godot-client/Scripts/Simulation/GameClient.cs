using System;
using System.Linq;
using Arch.Core;
using Game.Core.Extensions;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Services;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;
using Godot;
using GodotClient.Core.Autoloads;
using GodotClient.ECS;
using GodotClient.ECS.Components;

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
    
    private INetworkManager? _network;
    private ClientGameSimulation? _simulation;

    private int _localNetworkId = -1;
    private Label? _statusLabel;
    
    public Node2D EntitiesRoot => GetNode<Node2D>("Map/Entities");

    public override void _Ready()
    {
        base._Ready();
        
        Instance = this;

        CreateStatusLabel();

        // 1) Network vinda do Autoload (menu já inicializou)
        _network = NetworkClient.Instance.NetworkManager;

        // 2) Boot de simulação (fornece INetworkManager via DI para os sistemas que precisarem)
        _simulation = new ClientGameSimulation(_network);

        // 3) Carrega dados iniciais (NetworkId local, snapshot de mapa, etc.)
        LoadGameData();

        // 4) Registra handlers de rede apontando para a simulação
        RegisterPacketHandlers();

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
            
            _network.UnregisterPacketHandler<PlayerDataPacket>();
            _network.UnregisterPacketHandler<LeftPacket>();
            _network.UnregisterPacketHandler<PlayerStatePacket>();
            _network.UnregisterPacketHandler<PlayerVitalsPacket>();
            _network.UnregisterPacketHandler<CombatStatePacket>();
            _network.UnregisterPacketHandler<AttackResultPacket>();
        }

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
    
    private void SpawnPlayerVisual(in PlayerData data, bool isLocal)
    {
        
        
        GD.Print($"[GameClient] Spawning player visual for '{data.Name}' (NetID: {data.NetworkId}, Local: {isLocal})");
        
        var playerVisual = PlayerVisual.Create(isLocal);
        playerVisual.Name = $"Player_{data.NetworkId}";
        Entity entity = isLocal 
            ? _simulation!.SpawnLocalPlayer(data, playerVisual) 
            : _simulation!.SpawnRemotePlayer(data, playerVisual);

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
        
        _network.RegisterPacketHandler<PlayerDataPacket>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<LeftPacket>(HandlePlayerDespawn);
        _network.RegisterPacketHandler<PlayerStatePacket>(HandlePlayerState);
        _network.RegisterPacketHandler<PlayerVitalsPacket>(HandlePlayerVitals);
        _network.RegisterPacketHandler<CombatStatePacket>(HandleCombatState);
        _network.RegisterPacketHandler<AttackResultPacket>(HandleAttackResult);

        GD.Print("[GameClient] Packet handlers registered (ECS)");
    }

    private void HandlePlayerState(INetPeerAdapter peer, ref PlayerStatePacket packet)
    {
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
        int deltaX = Math.Abs(localPos.X - packet.Position.X);
        int deltaY = Math.Abs(localPos.Y - packet.Position.Y);
        
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
            _simulation.World.Get<Velocity>(entity) = packet.Velocity;
            _simulation.World.Get<Facing>(entity) = packet.Facing;
        }
    }

    private void HandlePlayerVitals(INetPeerAdapter peer, ref PlayerVitalsPacket packet)
    {
        if (_simulation is null)
            return;
        
        _simulation.ApplyPlayerVitals(packet.ToPlayerVitalsData());
        
        if (_simulation.TryGetPlayerVisual(packet.NetworkId, out var visual))
            visual.UpdateVitals(packet.Health.Current, packet.Health.Max, packet.Mana.Current, packet.Mana.Max);
        
        GD.Print($"[GameClient] Received PlayerVitalsPacket for NetworkId {packet.NetworkId}");
    }

    private void HandlePlayerSpawn(INetPeerAdapter peer, ref PlayerDataPacket data)
    {
        if (_simulation is null) return;
        var isLocal = data.NetworkId == _localNetworkId;
        var playerData = data.ToPlayerData();
        SpawnPlayerVisual(playerData, isLocal);
        UpdateStatus($"{data.Name} joined");
        
        GD.Print($"[GameClient] Spawned player '{data.Name}' (NetID: {data.NetworkId})");
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
        if (_simulation is null)
            return;

        // Localiza entidade do atacante
        if (!_simulation.TryGetPlayerEntity(packet.AttackerNetworkId, out var attackerEntity))
            return;

        // Se quiser armazenar algo temporário no ECS local para animação:
        var attackAnim = new AttackAction
        {
            DefenderNetworkId = packet.DefenderNetworkId,
            Type = packet.Type,
            RemainingDuration = packet.AttackDuration,
            WillHit = true, // ou false se quiser só visual; verdadeiro será sobrescrito pelo AttackResult
            Damage = 0      // será atualizado quando AttackResult chegar
        };
        _simulation.World.Add(attackerEntity, attackAnim);

        GD.Print($"[GameClient] CombatStatePacket received: Attacker={packet.AttackerNetworkId}, Defender={packet.DefenderNetworkId}, Type={packet.Type}, Duration={packet.AttackDuration}, Cooldown={packet.CooldownRemaining}");
    }

    private void HandleAttackResult(INetPeerAdapter peer, ref AttackResultPacket packet)
    {
        if (_simulation is null)
            return;

        // Atualiza efeitos no defensor (ex: pop de dano)
        if (_simulation.TryGetPlayerEntity(packet.DefenderNetworkId, out var defenderEntity))
        {
            // Aplicar efeitos visuais (ex: flash, números de dano)
            if (_simulation.TryGetPlayerVisual(packet.DefenderNetworkId, out var defenderVisual))
            {
                // Exemplo simples: troca cor rápida
                if (packet.WasHit)
                    defenderVisual.ChangeTempColor(Colors.Red, 0.2f);
                
                defenderVisual.CreateFloatingDamageLabel(packet.Damage, packet.IsCritical);
            }
        }

        // Atualiza AttackAction do atacante (se quiser refletir dano final)
        if (_simulation.TryGetPlayerEntity(packet.AttackerNetworkId, out var attackerEntity) &&
            _simulation.World.TryGet(attackerEntity, out AttackAction action))
        {
            action.Damage = packet.Damage;
            action.WillHit = packet.WasHit;
            _simulation.World.Set(attackerEntity, action);
        }

        GD.Print($"[GameClient] AttackResultPacket: Att={packet.AttackerNetworkId} Def={packet.DefenderNetworkId} Dmg={packet.Damage} Hit={packet.WasHit} Crit={packet.IsCritical}");
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

    private void CreateStatusLabel()
    {
        var hudLayer = new CanvasLayer { Name = "HudLayer", Layer = 100 };

        _statusLabel = new Label
        {
            Name = "StatusLabel",
            Position = new Vector2(12, 12),
            Text = "Loading game..."
        };
        _statusLabel.AddThemeColorOverride("font_color", Colors.White);

        hudLayer.AddChild(_statusLabel);
        AddChild(hudLayer);
    }

    private void UpdateStatus(string message)
    {
        if (_statusLabel is not null)
            _statusLabel.Text = message;
        
        GD.Print($"[GameClient] {message}");
    }
}