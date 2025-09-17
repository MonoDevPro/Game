using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Components;
using Simulation.Core.Options;

namespace Server.Console;

public class GameServerHost(IServiceProvider serviceProvider, ILogger<GameServerHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Game Server Host está a iniciar.");

        using var scope = serviceProvider.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<ISimulationBuilder<float>>();
        var worldOptions = scope.ServiceProvider.GetRequiredService<IOptions<WorldOptions>>().Value;

        var (simulationPipeline, world) = builder
            .WithWorldOptions(worldOptions)
            .WithRootServices(scope.ServiceProvider)
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            simulationPipeline.Update(0.016f);
            await Task.Delay(15, stoppingToken);
        }
    }
}
