using System;
using Arch.Core;
using Game.ECS.Entities.Factories;
using Game.ECS.Examples;
using Game.ECS.Services;
using Game.Network.Abstractions;
using Godot;
using GodotClient.Autoloads;
using GodotClient.Core.Autoloads;
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
    private ClientGameSimulation? _simulation;

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
        _simulation = new ClientGameSimulation();

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

            _network.UnregisterPacketHandler<PlayerPositionSnapshot>();
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
        var mapSnap = gameState.CurrentGameData?.MapDto;
        if (mapSnap is null)
        {
            GD.PushError("[GameClient] Map data is null! Returning to menu...");
            UpdateStatus("Error: Map data is null");
            SceneManager.Instance.LoadMainMenu();
            return;
        }

        // Constrói serviços de mapa (dados + spatial + cache) para uso em sistemas locais (opcional)

        var mapGrid = MapGrid.LoadMapGrid(mapSnap.Value);
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

        _network.RegisterPacketHandler<PlayerPositionSnapshot>(HandlePlayerInputSnapshot);
        _network.RegisterPacketHandler<PlayerStateSnapshot>(HandlePlayerState);
        _network.RegisterPacketHandler<PlayerVitalsSnapshot>(HandlePlayerVitals);
        _network.RegisterPacketHandler<PlayerSnapshot>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<PlayerDespawnSnapshot>(HandlePlayerDespawn);

        GD.Print("[GameClient] Packet handlers registered (ECS)");
    }
    
    private void HandlePlayerInputSnapshot(INetPeerAdapter peer, ref PlayerPositionSnapshot packet)
    {
        if (_simulation is null) return;

        // Resolve entidade por NetworkId no ECS (mapeamento deve ser mantido em um serviço de índice no ECS)
        if (!TryResolveEntity(packet.NetworkId, out var e))
            return;

        // Aplica estado autoritativo (posição/facing/speed) na simulação
        _simulation.ApplyRemotePlayerInput(in packet);
    }

    private void HandlePlayerState(INetPeerAdapter peer, ref PlayerStateSnapshot packet)
    {
        if (_simulation is null) return;

        // Resolve entidade por NetworkId no ECS (mapeamento deve ser mantido em um serviço de índice no ECS)
        if (!TryResolveEntity(packet.NetworkId, out var e))
            return;

        // Aplica estado autoritativo (posição/facing/speed) na simulação
        _simulation.ApplyStateFromServer(e, packet);
    }

    private void HandlePlayerVitals(INetPeerAdapter peer, ref PlayerVitalsSnapshot packet)
    {
        if (_simulation is null) return;

        if (!TryResolveEntity(packet.NetworkId, out var e))
            return;

        _simulation.ApplyPlayerVitals(e, packet);
    }

    private void HandlePlayerSpawn(INetPeerAdapter peer, ref PlayerSnapshot snapshot)
    {
        if (_simulation is null) return;
        _simulation.SpawnPlayer(snapshot);
        UpdateStatus($"{snapshot.Name} joined");
    }

    private void HandlePlayerDespawn(INetPeerAdapter peer, ref PlayerDespawnSnapshot packet)
    {
        if (_simulation is null) return;

        if (TryResolveEntity(packet.NetworkId, out var e))
        {
            _simulation.DespawnPlayer(in packet);
        }

        if (packet.NetworkId == _localNetworkId)
        {
            GD.PushWarning("[GameClient] You have been disconnected from the server!");
            UpdateStatus("You have been disconnected from the server");
            DisconnectAndReturnToMenu();
        }
    }

    // ==================== Entity Index (NetworkId -> Entity) ====================

    private bool TryResolveEntity(int networkId, out Entity e)
    {
        if (_simulation is not null) 
            return _simulation.TryGetPlayerEntity(networkId, out e);
        
        e = default;
        return false;
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

        _simulation?.ClearSimulation();

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