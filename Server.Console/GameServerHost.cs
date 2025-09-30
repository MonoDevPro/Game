using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS;
using Simulation.Core.Ports.Network;
using Application.Abstractions.Options;
using Microsoft.Extensions.DependencyInjection;

namespace Server.Console;

/// <summary>
/// Hospeda a execução do servidor do jogo, gerenciando o ciclo de vida da simulação e da rede.
/// </summary>
public class GameServerHost(
    IServiceProvider serviceProvider,
    ILogger<GameServerHost> logger)
    : BackgroundService
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
        
        // Inicia o gerenciador de rede para aceitar conexões.
        networkManager.Start();
        logger.LogInformation("Servidor de rede iniciado em {Authority}", networkManager.Authority);
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                // Processa todos os eventos de rede (novas conexões, dados recebidos, desconexões).
                networkManager.PollEvents();
                
                // Garante que a simulação avance em passos de tempo fixos.
                // Essencial para um servidor autoritativo ter um comportamento determinístico.
                groupSystems.Update(0.016f);
                
                // Libera o thread para outras tarefas, evitando consumo de 100% da CPU.
                await Task.Delay(15, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Operação cancelada. O Game Server Host está a parar.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Ocorreu um erro crítico no Game Server Host.");
        }
        finally
        {
            networkManager.Stop();
            groupSystems.Dispose();
            logger.LogInformation("Game Server Host parado e recursos de rede liberados.");
        }
    }
}