using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS;
using Simulation.Core.Options;

namespace Server.Console;

public class GameServerHost(IServiceProvider serviceProvider, ILogger<GameServerHost> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Game Server Host está a iniciar.");

        // Resolve as dependências necessárias para a simulação
        using var scope = serviceProvider.CreateScope();
        var builder = scope.ServiceProvider.GetRequiredService<ISimulationBuilder>();
        var worldOptions = scope.ServiceProvider.GetRequiredService<IOptions<WorldOptions>>().Value;
        var spatialOptions = scope.ServiceProvider.GetRequiredService<IOptions<SpatialOptions>>().Value;
        var networkOptions = scope.ServiceProvider.GetRequiredService<IOptions<NetworkOptions>>().Value;

        // Constrói a pipeline de simulação
        var simulationPipeline = builder
            .WithWorldOptions(worldOptions)
            .WithSpatialOptions(spatialOptions)
            .WithNetworkOptions(networkOptions)
            .WithRootServices(scope.ServiceProvider) // Passa o scope atual
            .Build();

        // Loop principal do jogo
        while (!stoppingToken.IsCancellationRequested)
        {
            simulationPipeline.Update(0.016f);
            await Task.Delay(15, stoppingToken);
        }
    }
}