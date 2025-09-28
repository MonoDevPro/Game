using System.Diagnostics;
using Simulation.Core.ECS;
using Simulation.Core.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.Ports.Network;
using Microsoft.Extensions.DependencyInjection;

namespace Simulation.Client;

/// <summary>
/// Hospeda a execução do cliente do jogo, gerenciando o ciclo de vida da simulação e da rede.
/// </summary>
public class GameClientHost(
    ILogger<GameClientHost> logger,
    ISimulationBuilder<float> simulationBuilder,
    INetworkManager networkManager,
    IOptions<WorldOptions> worldOptions,
    IOptions<AuthorityOptions> authorityOptions,
    IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    private const float FixedDeltaTime = 0.016f; // Aproximadamente 60 updates por segundo

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Game Client Host está a iniciar.");

        // O escopo é criado aqui para garantir que os serviços com escopo vivam durante toda a execução.
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        
        // Construir os sistemas ECS usando as dependências injetadas
        var groupSystems = simulationBuilder
            .WithAuthorityOptions(authorityOptions.Value)
            .WithWorldOptions(worldOptions.Value)
            .WithRootServices(scope.ServiceProvider) // Usamos o provedor de serviços do escopo criado
            .Build();

        // Inicia o gerenciador de rede
        networkManager.Start();
        logger.LogInformation("Cliente conectando ao servidor...");

        try
        {
            var stopwatch = new Stopwatch();
            var accumulator = 0.0;
            stopwatch.Start();

            while (!stoppingToken.IsCancellationRequested)
            {
                var elapsedTime = stopwatch.Elapsed.TotalSeconds;
                stopwatch.Restart();
                accumulator += elapsedTime;

                // Processa todos os eventos de rede pendentes
                networkManager.PollEvents();

                // Atualiza a simulação com um passo de tempo fixo para garantir consistência.
                // O loop while garante que a simulação se mantenha em dia mesmo que haja picos de lag.
                while (accumulator >= FixedDeltaTime)
                {
                    groupSystems.BeforeUpdate(FixedDeltaTime);
                    groupSystems.Update(FixedDeltaTime);
                    groupSystems.AfterUpdate(FixedDeltaTime);
                    accumulator -= FixedDeltaTime;
                }

                // Aguarda de forma eficiente até o próximo ciclo, sem alocar memória.
                await Task.Yield();
            }
        }
        catch (OperationCanceledException)
        {
            // Esperado quando o host está parando.
            logger.LogInformation("Operação cancelada. O Game Client Host está a parar.");
        }
        catch (Exception ex)
        {
            logger.LogCritical(ex, "Ocorreu um erro crítico no Game Client Host.");
        }
        finally
        {
            networkManager.Stop();
            groupSystems.Dispose();
            logger.LogInformation("Game Client Host parado e recursos de rede liberados.");
        }
    }
}