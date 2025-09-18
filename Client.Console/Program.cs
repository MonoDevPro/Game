using Arch.Core;
using Client.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Options;
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
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));
        
        services.AddNetworking();
        
        services.AddSingleton<ISimulationBuilder<float>, ClientSimulationBuilder>();

        services.AddSingleton<IWorldStaging, WorldStagingClient>();
    })
    .Build();

// --- Inicialização da Simulação ---
var logger = host.Services.GetRequiredService<ILogger<Program>>();
logger.LogInformation("Cliente a iniciar...");

var builder = host.Services.GetRequiredService<ISimulationBuilder<float>>();
var worldOptions = host.Services.GetRequiredService<IOptions<WorldOptions>>().Value;

var mapDatas = DataSeeder.GetMapSeeds();
var mapService = MapService.CreateFromTemplate(mapDatas.First(a => a.Id == 1));

var build = builder
    .WithWorldOptions(worldOptions)
    .WithMapService(mapService)
    .WithRootServices(host.Services)
    .Build();

logger.LogInformation("Pipeline de simulação do cliente construída. A entrar no loop principal.");

new ClientGameLoop(build.World, build.Systems).Run();

namespace Client.Console
{
    public class ClientGameLoop
    {
        private readonly World _world;
        private readonly GroupSystems _group;
        private Entity? _localPlayer;
        private DateTime _lastStateLog = DateTime.UtcNow;

        public ClientGameLoop(World world, GroupSystems group)
        {
            _world = world;
            _group = group;
        }

        public void Run()
        {
            var players = DataSeeder.GetPlayerSeeds();
        
            var player = players.First(a => a.Id == 1);
            _localPlayer = EntityFactorySystem.CreatePlayerEntity(_world, player);
        
            while (true)
            {
                _group.Update(0.016f); // Aproximadamente 60 FPS

                // Loga o estado do jogador local a cada 5 segundos
                if ((DateTime.UtcNow - _lastStateLog).TotalSeconds >= 0.5f)
                {
                    _world.Add<Input>(_localPlayer.Value,
                        new Input(IntentFlags.Move, InputFlags.Left));
                
                    if (_localPlayer.HasValue && _world.IsAlive(_localPlayer.Value))
                    {
                        var position = _world.Get<Position>(_localPlayer.Value);
                        var health = _world.Get<Health>(_localPlayer.Value);
                        System.Console.WriteLine($"[Estado do Jogador] Posição: ({position.X}, {position.Y}), Vida: {health.Current}/{health.Max}");
                    }
                    else
                    {
                        System.Console.WriteLine("Jogador local ainda não atribuído ou não existe.");
                    }
                    _lastStateLog = DateTime.UtcNow;
                }

                // Simula um atraso para evitar uso excessivo da CPU
                Thread.Sleep(10);
            }
        }
    }
}