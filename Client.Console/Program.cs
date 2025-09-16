using Microsoft.Extensions.Logging;
using Simulation.Core.ECS;
using Simulation.Core.Options;
using Microsoft.Extensions.Options;
using Arch.Core;
using Client.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Arch.System;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Staging.Player;
using Simulation.Core.Persistence.Models;
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

    services.AddSingleton<IWorldStaging, WorldStagingClient>();
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
        // Cria a entidade do jogador local (será substituída pela entidade real do servidor após o login/registro)
        var playerData = new PlayerData
        {
            Id = 1,
            MapId = 1,
            Name = _registerMode ? "NewPlayer" : "ExistingPlayer",
            Gender = Gender.Male,
            Vocation = Vocation.Mage,
            PosX = 0,
            PosY = 0,
            HealthMax = 100,
            HealthCurrent = 100,
            AttackCastTime = 1.0f,
            AttackCooldown = 1.5f,
            AttackDamage = 10,
            AttackRange = 1,
            MoveSpeed = 0.5f
        };
        
        var entity = _world.Create(new NewlyCreated());
        _localPlayer = _world.CreatePlayerEntity(entity, playerData);
        
        while (true)
        {
            _pipeline.Update(0.016f); // Aproximadamente 60 FPS

            // Loga o estado do jogador local a cada 5 segundos
            if ((DateTime.UtcNow - _lastStateLog).TotalSeconds >= 0.5f)
            {
                _world.Add<InputComponent>(_localPlayer.Value,
                    new InputComponent(IntentFlags.Move, InputFlags.Left));
                
                if (_localPlayer.HasValue && _world.IsAlive(_localPlayer.Value))
                {
                    var position = _world.Get<Position>(_localPlayer.Value);
                    var health = _world.Get<Health>(_localPlayer.Value);
                    Console.WriteLine($"[Estado do Jogador] Posição: ({position.X}, {position.Y}), Vida: {health.Current}/{health.Max}");
                }
                else
                {
                    Console.WriteLine("Jogador local ainda não atribuído ou não existe.");
                }
                _lastStateLog = DateTime.UtcNow;
            }

            // Simula um atraso para evitar uso excessivo da CPU
            Thread.Sleep(10);
        }
    }
}