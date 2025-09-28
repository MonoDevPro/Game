using Simulation.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Components.Data;
using Simulation.Core.ECS.Services;
using Simulation.Core.Options;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Client.ECS;
using Simulation.Core.ECS.Utils;
using Simulation.Network;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));
        services.Configure<AuthorityOptions>(context.Configuration.GetSection(AuthorityOptions.SectionName));
        
        services.AddLogging(configure => configure.AddConsole());
        
        services.AddSingleton(TimeProvider.System);
        
        // Client doesn't need map data initially - it should receive from server
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
        services.AddSingleton(MapService.CreateFromTemplate(mapData));
        services.AddSingleton<IWorldSaver, ClientWorldSaver>();
        
        services.AddNetworking();
        
        services.AddSingleton<ISimulationBuilder<float>, ClientSimulationBuilder>();

        services.AddHostedService<GameClientHost>();
        
    })
    .Build();

await host.RunAsync();

namespace Simulation.Client
{
    public class ClientWorldSaver : IWorldSaver
    {
        public void StageSave(PlayerData data)
        {
            // Cliente não precisa salvar dados - o servidor é responsável por isso
            Console.WriteLine($"Cliente recebeu dados do jogador: {data.Id}");
        }
    }
}