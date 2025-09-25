using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Server.Console.Hosted;

[Obsolete("Obsolet")]
public class QueuedHostedService(IServiceProvider serviceProvider, BackgroundTaskQueue taskQueue, ILogger<QueuedHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            // Aguarda por uma nova tarefa na fila
            var workItem = await taskQueue.DequeueAsync(stoppingToken);

            // O escopo é criado AQUI, de forma padronizada para cada item da fila.
            await using var scope = serviceProvider.CreateAsyncScope();
            try
            {
                await workItem(scope.ServiceProvider, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erro executando tarefa em segundo plano.");
            }
        }
    }
}