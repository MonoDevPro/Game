using System;
using System.Linq;
using Game.Network;
using Game.Network.Abstractions;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GodotClient.Autoloads;

/// <summary>
/// Gerenciador de rede do client Godot.
/// Inicializa e gerencia o ciclo de vida do sistema de networking do jogo.
/// Autor: MonoDevPro
/// Data: 2025-01-11 14:36:00
/// </summary>
public partial class NetworkClient : Node
{
    private static NetworkClient? _instance;
    public static NetworkClient Instance => _instance ?? throw new InvalidOperationException("NetworkClient not initialized");

    private INetworkManager? _networkManager;
    private ILogger<NetworkClient>? _logger;

    public INetworkManager NetworkManager => _networkManager ?? throw new InvalidOperationException("NetworkClient is not initialized.");

    public bool TryGetLocalPlayerNetworkId(out int networkId)
    {
        if (_networkManager is not null && 
            _networkManager.IsRunning && 
            _networkManager.Peers.PeerCount > 0)
        {
            networkId = _networkManager.Peers.GetAllPeers().First().RemoteId;
            return true;
        }
        networkId = -1;
        return false;
    }
    
    public override void _Ready()
    {
        base._Ready();
        _instance = this;
        
        _logger = ServicesManager.Instance.GetRequiredService<ILogger<NetworkClient>>();
        _networkManager = ServicesManager.Instance.GetRequiredService<INetworkManager>();
    }

    /// <summary>
    /// Inicia o cliente de rede.
    /// </summary>
    public void Start()
    {
        if (!NetworkManager.IsRunning)
        {
            _logger?.LogInformation("Starting network client");
            NetworkManager.Initialize();
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        _networkManager?.PollEvents();
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        if (_networkManager is not null && _networkManager.IsRunning)
        {
            _logger?.LogInformation("Stopping network client");
            _networkManager.Stop();
        }
    }
}