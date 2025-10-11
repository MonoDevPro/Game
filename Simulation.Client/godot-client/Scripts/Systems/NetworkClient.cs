using System;
using Game.Network;
using Game.Network.Abstractions;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GodotClient.Systems;

/// <summary>
/// Gerenciador de rede do client Godot.
/// Inicializa e gerencia o ciclo de vida do sistema de networking do jogo.
/// Autor: MonoDevPro
/// Data: 2025-01-11 14:36:00
/// </summary>
public partial class NetworkClient : Node
{
    private IServiceProvider? _serviceProvider;
    private INetworkManager? _networkManager;
    private ILogger<NetworkClient>? _logger;

    public INetworkManager NetworkManager => _networkManager ?? throw new InvalidOperationException("NetworkClient is not initialized.");

    /// <summary>
    /// Inicializa o sistema de rede com as opções fornecidas.
    /// </summary>
    public INetworkManager Initialize(NetworkOptions options)
    {
        if (_networkManager is not null)
        {
            return _networkManager;
        }

        var services = new ServiceCollection();
        services.AddSingleton(options);
        services.AddLogging(builder =>
        {
            builder.AddConsole();
#if DEBUG
            builder.SetMinimumLevel(LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
        });

        services.AddNetworking(options);

        _serviceProvider = services.BuildServiceProvider();
        _networkManager = _serviceProvider.GetRequiredService<INetworkManager>();
        _logger = _serviceProvider.GetService<ILogger<NetworkClient>>();

        return _networkManager;
    }

    /// <summary>
    /// Inicia o cliente de rede.
    /// </summary>
    public void Start()
    {
        if (_networkManager is null)
        {
            throw new InvalidOperationException("NetworkClient must be initialized before starting.");
        }

        if (!_networkManager.IsRunning)
        {
            _logger?.LogInformation("Starting network client");
            _networkManager.Start();
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

        if (_serviceProvider is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public T GetRequiredService<T>() where T : notnull
    {
        if (_serviceProvider is null)
        {
            throw new InvalidOperationException("Service provider not initialized.");
        }

        return _serviceProvider.GetRequiredService<T>();
    }
}