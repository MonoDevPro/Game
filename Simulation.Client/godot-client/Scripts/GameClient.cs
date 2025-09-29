using Godot;
using System;
using Arch.Core;
using System.Diagnostics;
using Application.Models.Options;
using GodotClient.API;
using Simulation.Core.ECS;
using Simulation.Core.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS.Components.Data;
using Simulation.Core.ECS.Utils;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Ports.Network;
using Simulation.Network;

namespace GodotClient;

public partial class GameClient : Node
{
    public static GameClient Instance { get; private set; } = null!;
    public int LocalPlayerId { get; private set; } = 999; // MVP: id fixo de teste
    public World World => _group?.WorldContext!;
    
    private IServiceProvider _services = null!;
    private INetworkManager _net = null!;
    private SystemGroup _group = null!;
    private readonly Stopwatch _stopwatch = new();
    private double _accumulator;
    private const float FixedDt = 0.016f; // ~60Hz
    
    private bool _initialized;

    public override void _EnterTree()
    {
        Instance = this;
    }

    public override void _Ready()
    {
        GD.Print("[GameClient] Waiting for config...");
        
        
        var cfg = GetNode<ConfigManager>("/root/ConfigManager");
        // Se já estiver carregada, inicializa imediatamente
        if (cfg.IsReady)
        {
            InitializeClient(cfg);
            return;
        }
        
        // Conectar sinal Godot (vai rodar no main thread)
        cfg.Connect(ConfigManager.SignalName.ConfigUpdated, Callable.From(OnConfigUpdated));
        // Opcional: conectar também ao evento C#
        cfg.ConfigAvailable += OnConfigAvailable;
    }
    
    private void OnConfigUpdated()
    {
        // desconecta o sinal para evitar múltiplas chamadas
        var cfg = GetNode<ConfigManager>("/root/ConfigManager");
        cfg.Disconnect(ConfigManager.SignalName.ConfigUpdated, Callable.From(OnConfigUpdated));
        cfg.ConfigAvailable -= OnConfigAvailable;

        InitializeClient(cfg);
    }
    
    private void OnConfigAvailable()
    {
        // caso queira suportar a notificação por C# event
        var cfg = GetNode<ConfigManager>("/root/ConfigManager");
        cfg.ConfigAvailable -= OnConfigAvailable;
        // Use CallDeferred para garantir a chamada segura no main thread do Godot
        CallDeferred(nameof(InitializeClientDeferred));
    }

    private void InitializeClientDeferred()
    {
        var cfg = GetNode<ConfigManager>("/root/ConfigManager");
        InitializeClient(cfg);
    }
    
    private void InitializeClient(ConfigManager configManager)
    {
        if (_initialized) return;
        _initialized = true;

        GD.Print("[GameClient] Initializing with config...",
            " World:", configManager.World,
            " Network:", configManager.Network,
            " Authority:", configManager.Authority);

        var sc = new ServiceCollection();

        // Logging
        sc.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Information));

        var worldOptions = configManager.World ?? new WorldOptions();
        var netOptions = configManager.Network ?? new NetworkOptions();
        var authorityOptions = configManager.Authority ?? new AuthorityOptions();

        sc.AddSingleton<IOptions<WorldOptions>>(_ => Options.Create(worldOptions));
        sc.AddSingleton<IOptions<NetworkOptions>>(_ => Options.Create(netOptions));
        sc.AddSingleton<IOptions<AuthorityOptions>>(_ => Options.Create(authorityOptions));

        // Relógio do sistema (caso necessário por algum serviço)
        sc.AddSingleton(TimeProvider.System);

        // Mapa (template simples como no headless client)
        var mapData = new MapData
        {
            Id = 1,
            Name = "ClientMap",
            Width = 100,
            Height = 100,
            BorderBlocked = true,
            CollisionRowMajor = new byte[100 * 100],
            TilesRowMajor = new TileType[100 * 100],
            UsePadded = false
        };
        sc.AddSingleton(MapService.CreateFromTemplate(mapData));

        // World saver vazio no cliente
        sc.AddSingleton<IWorldSaver, ClientWorldSaver>();

        // Networking + ECS Builder (lado do cliente)
        sc.AddNetworking();
        // sc.AddSingleton<ISimulationBuilder<float>, ClientSimulationBuilder>();
        sc.AddSingleton<ISimulationBuilder<float>, GodotClientSimulationBuilder>();

        _services = sc.BuildServiceProvider();

        // Construir pipeline ECS
        var builder = _services.GetRequiredService<ISimulationBuilder<float>>();
        _group = builder
            .WithAuthorityOptions(authorityOptions)
            .WithWorldOptions(worldOptions)
            .WithRootServices(_services)
            .Build();

        // Iniciar rede (conecta ao servidor)
        _net = _services.GetRequiredService<INetworkManager>();
        _net.Start();

        _stopwatch.Restart();
        _accumulator = 0.0;
        
        GD.Print("[GameClient] Initialized and connected (attempting) to server.");
    }

    public override void _Process(double delta)
    {
        if (!_initialized) return;
        
        // Processa eventos de rede
        _net.PollEvents();

        // Fixed-step ECS update
        var elapsed = _stopwatch.Elapsed.TotalSeconds;
        _stopwatch.Restart();
        _accumulator += elapsed;
        while (_accumulator >= FixedDt)
        {
            _group.BeforeUpdate(FixedDt);
            _group.Update(FixedDt);
            _group.AfterUpdate(FixedDt);
            _accumulator -= FixedDt;
        }
    }

    public override void _ExitTree()
    {
        try
        {
            _net?.Stop();
        }
        catch { /* ignore */ }
        try
        {
            _group?.Dispose();
        }
        catch { /* ignore */ }
        GD.Print("[GameClient] Shutdown complete.");

        if (Instance == this) Instance = null!;
    }

    private sealed class ClientWorldSaver : IWorldSaver
    {
        public void StageSave(PlayerData data)
        {
            GD.Print($"[ClientWorldSaver] StageSave called for player {data.Id}, but no action is taken on the client.");
        }
    }
}