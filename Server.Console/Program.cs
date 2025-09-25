using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Console;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Adapters;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Services;
using Simulation.Core.Options;
using Simulation.Core.Ports;
using Simulation.Network;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));
        services.Configure<AuthorityOptions>(context.Configuration.GetSection(AuthorityOptions.SectionName));
        
        services.AddLogging(configure => configure.AddConsole());
        
        services.AddSingleton(TimeProvider.System);
        
        var mapData = new MapData
        {
            Id = 1,
            Name = "TestMap",
            Width = 100,
            Height = 100,
            BorderBlocked = true,
            CollisionRowMajor = new byte[100 * 100],
            TilesRowMajor = new TileType[100 * 100],
            UsePadded = false
        };
        services.AddSingleton(MapService.CreateFromTemplate(mapData));
        services.AddSingleton<IWorldSaver, WorldSaver>();
        
        services.AddNetworking();
        
        services.AddSingleton<ISimulationBuilder<float>, ServerSimulationBuilder>();

        services.AddHostedService<GameServerHost>();
        
    })
    .Build();

await host.RunAsync();

public class WorldSaver : IWorldSaver
{
    public void StageSave(PlayerData data)
    {
        // Implementar a lógica de salvamento do mundo aqui
        Console.WriteLine($"Salvando dados do jogador: {data.Id}");
    }
}