using Microsoft.Extensions.Logging;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Client;
using Simulation.Core.Options;
using Microsoft.Extensions.Options;
using Arch.Core;
using Client.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Simulation.Core.ECS.Shared.Staging;
using Arch.System;
using Simulation.Core.ECS.Shared.Systems.Network;
using Simulation.Network;

// --- Configuração do Host ---
Console.Title = "CLIENT";

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration((context, config) =>
    {
        config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddNetworking();
        
        services.AddSingleton<ISimulationBuilder<float>, ClientSimulationBuilder>();
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));

        services.AddSingleton<IPlayerStagingArea, PlayerStagingArea>();
        services.AddSingleton<IMapStagingArea, MapStagingArea>();
    })
    .Build();

// --- Inicialização da Simulação ---
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Cliente a iniciar...");

var builder = host.Services.GetRequiredService<ISimulationBuilder<float>>();
var worldOptions = host.Services.GetRequiredService<IOptions<WorldOptions>>().Value;
var networkOptions = host.Services.GetRequiredService<IOptions<NetworkOptions>>().Value;


var build = builder
    .WithWorldOptions(worldOptions)
    .WithRootServices(host.Services)
    
    // --- Regista os mesmos componentes que o servidor ---
    // Isto é crucial para que o cliente saiba como desserializar os pacotes recebidos
    // e para enviar os seus próprios componentes de "intenção".
    .WithSynchronizedComponent<Position>(new SyncOptions { Authority = Authority.Server })
    .WithSynchronizedComponent<Health>(new SyncOptions { Authority = Authority.Server })
    //.WithSynchronizedComponent<StateComponent>(new SyncOptions { Authority = Authority.Server })
    .WithSynchronizedComponent<Direction>(new SyncOptions { Authority = Authority.Server })
    .WithSynchronizedComponent<InputComponent>(new SyncOptions { Authority = Authority.Client })
    
    .Build();

logger.LogInformation("Pipeline de simulação do cliente construída. A entrar no loop principal.");

bool registerMode = args.Any(a => a.Equals("--register", StringComparison.OrdinalIgnoreCase));
new ClientGameLoop(build.World, build.Group, registerMode).Run();

public class ClientGameLoop
{
    private readonly World _world;
    private readonly Group<float> _pipeline;
    private Entity? _localPlayer;
    private DateTime _lastStateLog = DateTime.UtcNow;

    private readonly bool _registerMode;

    public ClientGameLoop(World world, Group<float> pipeline, bool registerMode)
    {
        _world = world;
        _pipeline = pipeline;
        _registerMode = registerMode;
    }

    public void Run()
    {
        while (true)
        {
            return;
        }
    }
}