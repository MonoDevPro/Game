using System;
using Arch.Core;
using Game.Core.Maps;
using Game.ECS.Components;
using Game.Network.Abstractions;
using Game.Network.Packets.Simulation;
using Godot;
using GodotClient.Autoloads;
using Microsoft.Extensions.DependencyInjection;

namespace GodotClient.Simulation;

/// <summary>
/// Cliente principal do jogo orientado a ECS:
/// - Bootstrapa a ClientSimulation (ECS + sistemas)
/// - Carrega dados iniciais (mapa, ids) do GameStateManager
/// - Registra handlers de rede e encaminha snapshots para a simulação
/// - Roda o loop de simulação (fixed-timestep) em _Process
/// </summary>
public partial class GameScript : Node2D
{
    private INetworkManager? _network;
    private ClientSimulation? _simulation;

    private int _localNetworkId = -1;
    private Label? _statusLabel;

    public override void _Ready()
    {
        base._Ready();

        CreateStatusLabel();

        // 1) Network vinda do Autoload (menu já inicializou)
        _network = NetworkClient.Instance.NetworkManager;

        // 2) Boot de simulação (fornece INetworkManager via DI para os sistemas que precisarem)
        var provider = BuildServiceProvider(_network);
        _simulation = new ClientSimulation(provider);

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

            _network.UnregisterPacketHandler<PlayerStateSnapshot>();
            _network.UnregisterPacketHandler<PlayerVitalsSnapshot>();
            _network.UnregisterPacketHandler<PlayerSnapshot>();
            _network.UnregisterPacketHandler<PlayerDespawnSnapshot>();
        }

        GD.Print("[GameClient] Unloaded");
    }

    // ==================== Boot/DI ====================

    private static IServiceProvider BuildServiceProvider(INetworkManager? network)
    {
        var sc = new ServiceCollection();
        if (network is not null)
            sc.AddSingleton(network);
        return sc.BuildServiceProvider();
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
        var mapSnap = gameState.CurrentGameData?.MapSnapshot;
        if (mapSnap is null)
        {
            GD.PushError("[GameClient] Map data is null! Returning to menu...");
            UpdateStatus("Error: Map data is null");
            SceneManager.Instance.LoadMainMenu();
            return;
        }

        // Constrói serviços de mapa (dados + spatial + cache) para uso em sistemas locais (opcional)
        
        var mapGrid = MapSnapshotLoader.LoadMapGrid(mapSnap.Value);
        var spatial = new MapSpatial(0, 0, mapSnap.Value.Width, mapSnap.Value.Height);

        UpdateStatus($"Playing (NetID: {_localNetworkId})");

        // Limpa estado global após carregar (não precisamos manter o snapshot no autoload)
        gameState.ResetState();
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

        _network.RegisterPacketHandler<PlayerStateSnapshot>(HandlePlayerState);
        _network.RegisterPacketHandler<PlayerVitalsSnapshot>(HandlePlayerVitals);
        _network.RegisterPacketHandler<PlayerSnapshot>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<PlayerDespawnSnapshot>(HandlePlayerDespawn);

        GD.Print("[GameClient] Packet handlers registered (ECS)");
    }

    private void HandlePlayerState(INetPeerAdapter peer, ref PlayerStateSnapshot packet)
    {
        if (_simulation is null) return;

        // Resolve entidade por NetworkId no ECS (mapeamento deve ser mantido em um serviço de índice no ECS)
        if (!TryResolveEntity(packet.PlayerId, out var e))
            return;

        // Aplica estado autoritativo (posição/facing/speed) na simulação
        _simulation.ApplyStateFromServer(e, packet);
    }

    private void HandlePlayerVitals(INetPeerAdapter peer, ref PlayerVitalsSnapshot packet)
    {
        if (_simulation is null) return;

        if (!TryResolveEntity(packet.PlayerId, out var e))
            return;

        _simulation.ApplyPlayerVitals(e, packet);
    }

    private void HandlePlayerSpawn(INetPeerAdapter peer, ref PlayerSnapshot snapshot)
    {
        if (_simulation is null) return;

        // Cria/spawna o player na simulação e registra no índice
        // Observação: SpawnPlayer exige stats; PlayerSnapshot deve carregar dados suficientes para isso.
        // Ajuste a extração conforme a sua struct snapshot.
        var entity = _simulation.SpawnPlayer(
            playerId: snapshot.PlayerId,
            networkId: snapshot.NetworkId,
            spawnX: snapshot.PositionX,
            spawnY: snapshot.PositionY,
            spawnZ: snapshot.PositionZ,
            facingX: snapshot.FacingX,
            facingY: snapshot.FacingY,
            hp: snapshot.Hp,
            maxHp: snapshot.MaxHp,
            hpRegen: snapshot.HpRegen,
            mp: snapshot.Mp,
            maxMp: snapshot.MaxMp,
            mpRegen: snapshot.MpRegen,
            movementSpeed: (float)snapshot.MovementSpeed,
            attackSpeed: (float)snapshot.AttackSpeed,
            physicalAttack: snapshot.PhysicalAttack,
            magicAttack: snapshot.MagicAttack,
            physicalDefense: snapshot.PhysicalDefense,
            magicDefense: snapshot.MagicDefense
        );

        RegisterEntity(snapshot.NetworkId, entity);
        UpdateStatus($"{snapshot.Name} joined");
    }

    private void HandlePlayerDespawn(INetPeerAdapter peer, ref PlayerDespawnSnapshot packet)
    {
        if (_simulation is null) return;

        if (TryResolveEntity(packet.NetworkId, out var e))
        {
            _simulation.DespawnEntity(e);
            UnregisterEntity(packet.NetworkId);
        }

        if (packet.NetworkId == _localNetworkId)
        {
            GD.PushWarning("[GameClient] You have been disconnected from the server!");
            UpdateStatus("You have been disconnected from the server");
            DisconnectAndReturnToMenu();
        }
    }

    // ==================== Entity Index (NetworkId -> Entity) ====================

    // Nota: mantenha o índice real dentro da camada ECS (ex.: PlayerIndexService).
    // Aqui mantemos apenas um passthrough para facilitar o binding do handler -> ECS.
    // Substitua as implementações abaixo por forward calls ao seu serviço de índice.

    private readonly System.Collections.Generic.Dictionary<int, Entity> _entityByNetId = new();

    private void RegisterEntity(int networkId, Entity e)
    {
        _entityByNetId[networkId] = e;
    }

    private void UnregisterEntity(int networkId)
    {
        _entityByNetId.Remove(networkId);
    }

    private bool TryResolveEntity(int networkId, out Entity e)
    {
        return _entityByNetId.TryGetValue(networkId, out e);
    }

    // ==================== UI / Disconnect ====================

    public void DisconnectAndReturnToMenu()
    {
        GD.Print("[GameClient] Disconnecting...");

        UpdateStatus("Disconnecting...");

        // Para rede
        if (_network?.IsRunning == true)
        {
            _network.Stop();
        }

        // Limpa índice local de entidades
        _entityByNetId.Clear();

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
        {
            _statusLabel.Text = message;
        }
        GD.Print($"[GameClient] {message}");
    }
}