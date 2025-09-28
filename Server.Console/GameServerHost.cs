using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Services;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace Server.Console;

/// <summary>
/// Hospeda a execução do servidor do jogo, gerenciando o ciclo de vida da simulação e da rede.
/// </summary>
public class GameServerHost(
    ILogger<GameServerHost> logger,
    ISimulationBuilder<float> simulationBuilder,
    INetworkManager networkManager,
    IOptions<WorldOptions> worldOptions,
    IOptions<AuthorityOptions> authorityOptions,
    IServiceScopeFactory serviceScopeFactory)
    : BackgroundService
{
    private const float FixedDeltaTime = 0.016f; // Aproximadamente 60 updates por segundo (tick rate)

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Game Server Host está a iniciar.");

        // Cria um escopo de serviço para a execução do host.
        await using var scope = serviceScopeFactory.CreateAsyncScope();
        
        // Constrói os sistemas ECS com as dependências injetadas.
        var groupSystems = simulationBuilder
            .WithAuthorityOptions(authorityOptions.Value)
            .WithWorldOptions(worldOptions.Value)
            .WithRootServices(scope.ServiceProvider)
            .Build();

        // Inicia o gerenciador de rede para aceitar conexões.
        networkManager.Start();
        logger.LogInformation("Servidor iniciado e aguardando conexões.");

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

                // Processa todos os eventos de rede (novas conexões, dados recebidos, desconexões).
                networkManager.PollEvents();

                // Garante que a simulação avance em passos de tempo fixos.
                // Essencial para um servidor autoritativo ter um comportamento determinístico.
                while (accumulator >= FixedDeltaTime)
                {
                    groupSystems.BeforeUpdate(FixedDeltaTime);
                    groupSystems.Update(FixedDeltaTime);
                    groupSystems.AfterUpdate(FixedDeltaTime);
                    accumulator -= FixedDeltaTime;
                }

                // Libera o thread para outras tarefas, evitando consumo de 100% da CPU.
                await Task.Yield();
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