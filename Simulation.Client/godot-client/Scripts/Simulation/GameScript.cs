using System;
using Arch.Core;
using Game.Core.Extensions;
using Game.ECS.Services;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;
using Godot;
using GodotClient.Autoloads;
using GodotClient.Core.Autoloads;
using GodotClient.ECS;
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

            _network.UnregisterPacketHandler<PlayerJoinPacket>();
            _network.UnregisterPacketHandler<PlayerLeftPacket>();
            _network.UnregisterPacketHandler<PlayerStatePacket>();
            _network.UnregisterPacketHandler<PlayerVitalsPacket>();
            _network.UnregisterPacketHandler<PlayerDataPacket>();
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
        
        var mapGrid = new MapGrid(width, height, layers, collisionMasks);
        var spatial = new MapSpatial();
        
        _simulation?.RegisterMap(mapSnap.Value.MapId, mapGrid, spatial);
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
        
        _network.RegisterPacketHandler<PlayerDataPacket>(HandlePlayerSpawn);
        _network.RegisterPacketHandler<PlayerLeftPacket>(HandlePlayerDespawn);
        _network.RegisterPacketHandler<PlayerStatePacket>(HandlePlayerState);
        _network.RegisterPacketHandler<PlayerVitalsPacket>(HandlePlayerVitals);

        GD.Print("[GameClient] Packet handlers registered (ECS)");
    }
    

    private void HandlePlayerState(INetPeerAdapter peer, ref PlayerStatePacket packet)
    {
        // Aplica estado autoritativo (posição/facing/speed) na simulação
        _simulation?.ApplyPlayerState(packet.ToPlayerStateData());
    }

    private void HandlePlayerVitals(INetPeerAdapter peer, ref PlayerVitalsPacket packet)
    {
        _simulation?.ApplyPlayerVitals(packet.ToPlayerVitalsData());
    }

    private void HandlePlayerSpawn(INetPeerAdapter peer, ref PlayerDataPacket data)
    {
        if (_simulation is null) return;
        
        var playerVisual = new PlayerVisual();
        playerVisual.Name = $"Player_{data.NetworkId}";
        
        Entity entity = data.NetworkId == _localNetworkId 
            ? _simulation.SpawnLocalPlayer(data.ToPlayerData(), playerVisual) 
            : _simulation.SpawnRemotePlayer(data.ToPlayerData(), playerVisual);
        
        UpdateStatus($"{data.Name} joined");
    }
    
    private void HandlePlayerDespawn(INetPeerAdapter peer, ref PlayerLeftPacket packet)
    {
        _simulation?.DespawnPlayer(packet.NetworkId);
        if (packet.NetworkId == _localNetworkId)
        {
            GD.PushWarning("[GameClient] You have been disconnected from the server!");
            UpdateStatus("You have been disconnected from the server");
            DisconnectAndReturnToMenu();
        }
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
        {
            _statusLabel.Text = message;
        }
        GD.Print($"[GameClient] {message}");
    }
}