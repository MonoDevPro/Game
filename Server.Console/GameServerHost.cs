using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Shared;
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
        var spatialOptions = scope.ServiceProvider.GetRequiredService<IOptions<SpatialOptions>>().Value;
        var networkOptions = scope.ServiceProvider.GetRequiredService<IOptions<NetworkOptions>>().Value;

        var (simulationPipeline, world) = builder
            .WithWorldOptions(worldOptions)
            .WithSpatialOptions(spatialOptions)
            .WithNetworkOptions(networkOptions)
            .WithRootServices(scope.ServiceProvider)
            
            .WithSynchronizedComponent<Position>(new SyncOptions { Authority = Authority.Server, Trigger = SyncTrigger.OnChange })
            .WithSynchronizedComponent<Health>(new SyncOptions { Authority = Authority.Server, Trigger = SyncTrigger.OnChange })
            .WithSynchronizedComponent<StateComponent>(new SyncOptions { Authority = Authority.Server, Trigger = SyncTrigger.OnChange })
            .WithSynchronizedComponent<Direction>(new SyncOptions { Authority = Authority.Server, Trigger = SyncTrigger.OnChange })
            .WithSynchronizedComponent<InputComponent>(new SyncOptions { Authority = Authority.Client })
            
            .Build();

        while (!stoppingToken.IsCancellationRequested)
        {
            simulationPipeline.Update(0.016f);
            await Task.Delay(15, stoppingToken);
        }
    }
}
