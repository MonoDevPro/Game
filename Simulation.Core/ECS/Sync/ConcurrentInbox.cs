using System.Collections.Concurrent;

namespace Simulation.Core.ECS.Sync;

public class ConcurrentInbox<T> where T : notnull
{
    private readonly ConcurrentQueue<T> _queue = new();
    
    public void Enqueue(T item) => _queue.Enqueue(item);
    
    public bool TryDequeue(out T item) => _queue.TryDequeue(out item!);
}
