using System;
using Game.Network;
using Game.Network.Abstractions;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GodotClient;

public partial class ApiClient : Node
{
    private IServiceProvider? _serviceProvider;
    private INetworkManager? _networkManager;
    private ILogger<ApiClient>? _logger;

    public INetworkManager NetworkManager => _networkManager ?? throw new InvalidOperationException("ApiClient is not initialized.");

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
        _logger = _serviceProvider.GetService<ILogger<ApiClient>>();

        return _networkManager;
    }

    public void Start()
    {
        if (_networkManager is null)
        {
            throw new InvalidOperationException("ApiClient must be initialized before starting.");
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