using System.Collections.Concurrent;
using Simulation.Core.Server.Persistence.Contracts;

namespace Server.Persistence.Hosted;
/// <summary>
/// Implementação thread-safe de uma fila de tarefas em segundo plano.
/// </summary>
public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    // ConcurrentQueue é uma estrutura de dados otimizada para múltiplos produtores e consumidores.
    private readonly ConcurrentQueue<Func<IServiceProvider, CancellationToken, ValueTask>> _workItems = new();
    
    // SemaphoreSlim é usado para sinalizar de forma eficiente que um novo item foi adicionado à fila,
    // evitando que o serviço consumidor (BackgroundService) precise verificar a fila constantemente (polling).
    private readonly SemaphoreSlim _signal = new(0);

    /// <summary>
    /// Obtém o número atual de itens na fila.
    /// </summary>
    public int Count => _workItems.Count;

    /// <summary>
    /// Enfileira uma nova tarefa.
    /// </summary>
    public void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, ValueTask> workItem)
    {
        if (workItem is null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        _workItems.Enqueue(workItem);
        
        // Libera o semáforo, incrementando sua contagem. Isso sinaliza ao DequeueAsync
        // que um item está pronto e que ele pode prosseguir.
        _signal.Release();
    }

    /// <summary>
    /// Aguarda e retira uma tarefa da fila.
    /// </summary>
    public async ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        // Aguarda de forma assíncrona até que o semáforo seja liberado (contagem > 0).
        // Isso significa que o thread não fica bloqueado, sendo liberado para outras tarefas.
        await _signal.WaitAsync(cancellationToken);

        _workItems.TryDequeue(out var workItem);

        // Se o workItem for nulo aqui, é um estado inesperado e indica um erro de lógica.
        if (workItem is null)
        {
            throw new InvalidOperationException("Dequeued a null work item. This should not happen.");
        }
        
        return workItem;
    }
}