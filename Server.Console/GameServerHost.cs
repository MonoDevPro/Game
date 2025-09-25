using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Services;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;

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
        var authorityOptions = scope.ServiceProvider.GetRequiredService<IOptions<AuthorityOptions>>().Value;
        
        var groupSystems = builder
            .WithAuthorityOptions(authorityOptions)
            .WithWorldOptions(worldOptions)
            .WithRootServices(scope.ServiceProvider)
            .Build();
        
        var networkManager = scope.ServiceProvider.GetRequiredService<INetworkManager>();
        
        while (!stoppingToken.IsCancellationRequested)
        {
            networkManager.PollEvents();
            
            groupSystems.Update(0.016f);
            
            await Task.Delay(15, stoppingToken);
        }
    }
}