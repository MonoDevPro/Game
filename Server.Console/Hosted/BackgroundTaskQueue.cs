using System.Collections.Concurrent;

namespace Server.Console.Hosted;

[Obsolete("Obsolet")]
public class BackgroundTaskQueue
{
    private readonly ConcurrentQueue<Func<IServiceProvider, CancellationToken, ValueTask>> _workItems = new();
    private readonly SemaphoreSlim _signal = new(0);
    
    public int Count => _workItems.Count;

    public void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, ValueTask> workItem)
    {
        if (workItem is null)
            throw new ArgumentNullException(nameof(workItem));

        _workItems.Enqueue(workItem);
        _signal.Release();
    }

    /// <summary>
    /// Aguarda e retira uma tarefa da fila.
    /// </summary>
    public async ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken)
    {
        await _signal.WaitAsync(cancellationToken);
        _workItems.TryDequeue(out var workItem);

        if (workItem is null)
            throw new InvalidOperationException("Dequeued a null work item. This should not happen.");
        
        return workItem;
    }
}