using Arch.Core;
using Client.Console;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Auth.Messages;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Network.Contracts;
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


// Test Auth Server
var processor = host.Services
    .GetRequiredService<IChannelProcessorFactory>()
    .CreateOrGet(NetworkChannel.Authentication);
processor.RegisterHandler<LoginResponse>( (peer, packet) =>
{
    if (packet.Success)
        logger.LogInformation("AccountId: {AccountId}, Token: {Token}, Message: {Username}", packet.AccountId, packet.SessionToken, packet.Message);
    else
        logger.LogWarning("Falha no login: {Message}", packet.Message);
});
processor.RegisterHandler<RegisterResponse>( (peer, packet) =>
{
    if (packet.Success)
        logger.LogInformation("Sucesso no login: {Message}", packet.Message);
    else
        logger.LogWarning("Falha no login: {Message}", packet.Message);
});
    
processor.SendToServer(new LoginRequest("Filipe", "123456"), NetworkDeliveryMethod.ReliableOrdered);

new ClientGameLoop(build.World, build.Systems, processor).Run();

namespace Client.Console
{
    public class ClientGameLoop(World world, GroupSystems group, IChannelEndpoint endpoint)
    {
        private readonly World _world = world;
        private Entity? _localPlayer;
        private DateTime _lastStateLog = DateTime.UtcNow;

        public void Run()
        {
            
            group.Update(0.016f); // Aproximadamente 60 FPS
            Thread.Sleep(500);
            group.Update(0.016f); // Aproximadamente 60 FPS
            
            endpoint.SendToServer(new RegisterRequest("Filipe", "123456"), NetworkDeliveryMethod.ReliableOrdered);
            Thread.Sleep(500);
            group.Update(0.016f); // Aproximadamente 60 FPS
            Thread.Sleep(500);
            group.Update(0.016f); // Aproximadamente 60 FPS
            endpoint.SendToServer(new LoginRequest("Filipe", "123456"), NetworkDeliveryMethod.ReliableOrdered);
            Thread.Sleep(500);
            group.Update(0.016f); // Aproximadamente 60 FPS
            Thread.Sleep(500);
            group.Update(0.016f); // Aproximadamente 60 FPS
            
            endpoint.SendToServer(new LoginRequest("Filipe", "123456"), NetworkDeliveryMethod.ReliableOrdered);
            Thread.Sleep(500);
            group.Update(0.016f); // Aproximadamente 60 FPS
            Thread.Sleep(500);
            group.Update(0.016f); // Aproximadamente 60 FPS
            
            //var players = DataSeeder.GetPlayerSeeds();
        
            //var player = players.First(a => a.Id == 1);
            //_localPlayer = EntityFactorySystem.CreatePlayerEntity(_world, player);
            while (true)
            {
                group.Update(0.016f); // Aproximadamente 60 FPS

                // Loga o estado do jogador local a cada 5 segundos
                /*if ((DateTime.UtcNow - _lastStateLog).TotalSeconds >= 0.5f)
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
                }*/

                // Simula um atraso para evitar uso excessivo da CPU
                Thread.Sleep(10);
            }
        }
    }
}