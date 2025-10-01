using GameWeb.Application.Common.Options;
using GameWeb.Application.Players.Models;
using Server.Console;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS;
using Simulation.Core.Ports.ECS;
using Simulation.Core.Server.ECS;
using Simulation.Network;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));
        services.Configure<AuthorityOptions>(context.Configuration.GetSection(AuthorityOptions.SectionName));
        
        services.AddLogging(configure => configure.AddConsole());
        
        services.AddSingleton(TimeProvider.System);
        
        services.AddSingleton<IWorldSaver, WorldSaver>();
        
        services.AddNetworking();
        
        services.AddSingleton<ISimulationBuilder<float>, ServerSimulationBuilder>();
        
        services.AddHostedService<GameServerHost>();
        
        // TODO: Falta obter isso da api pra funcionar e rodar.
        // services.AddSingleton(MapService.CreateFromTemplate(mapData));
        
    })
    .Build();

await host.RunAsync();

namespace Server.Console
{
    public class WorldSaver : IWorldSaver
    {
        public void StageSave(PlayerDto dto)
        {
            // Implementar a lógica de salvamento do mundo aqui
            System.Console.WriteLine($"Salvando dados do jogador: {dto.Id}");
        }
    }
}