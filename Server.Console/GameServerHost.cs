using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Services;
using Simulation.Core.Options;

namespace Server.Console;

public class GameServerHost(IServiceProvider serviceProvider, ILogger<GameServerHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Game Server Host está a iniciar.");
        
        await using var scope = serviceProvider.CreateAsyncScope();
        
        // ECS Builder e Opções do Mundo
        var builder = scope.ServiceProvider.GetRequiredService<ISimulationBuilder<float>>();
        var worldOptions = scope.ServiceProvider.GetRequiredService<IOptions<WorldOptions>>().Value;
        var mapService = scope.ServiceProvider.GetRequiredService<MapService>();
        var authorityOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthorityOptions>>().Value;
        
        var (groupSystems, world, worldManager) = builder
            .WithAuthorityOptions(authorityOptions)
            .WithMapService(mapService)
            .WithWorldOptions(worldOptions)
            .WithRootServices(scope.ServiceProvider)
            .Build();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            groupSystems.Update(0.016f);
            
            await Task.Delay(15, stoppingToken);
        }
    }
}