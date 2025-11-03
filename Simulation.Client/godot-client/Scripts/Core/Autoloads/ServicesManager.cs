using System;
using Game.Network;
using Godot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GodotClient.Core.Autoloads;

public sealed partial class ServicesManager : Node
{
    private static ServicesManager? _instance;
    public static ServicesManager Instance => _instance ?? throw new InvalidOperationException("ServicesManager not initialized");

    private IServiceProvider? _provider;
    
    public override void _Ready()
    {
        base._Ready();
        
        var services = new ServiceCollection();
        services.AddLogging(builder =>
        {
            builder.AddConsole();
#if DEBUG
            builder.SetMinimumLevel(LogLevel.Debug);
#else
            builder.SetMinimumLevel(LogLevel.Information);
#endif
        });

        var netOptions = ConfigManager.Instance.CreateNetworkOptions();
        services.AddSingleton(GetTree());
        services.AddSingleton(netOptions);
        services.AddNetworking(netOptions);
        _provider = services.BuildServiceProvider();
        _instance = this;
        GD.Print("[ServicesManager] Initialized");
    }
    
    public T GetRequiredService<T>() where T : notnull
    {
        if (_provider is null)
            throw new InvalidOperationException("Service provider not initialized.");

        return _provider.GetRequiredService<T>();
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        _instance = null;
    }
}