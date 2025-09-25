using System.Collections.Concurrent;

namespace Simulation.Core.ECS.Sync;

public interface IInbox<in T>
{
    void Enqueue(T item);
}
public interface IOutbox<T>
{
    bool TryDequeue(out T item);
}

public class ConcurrentInbox<T> : IInbox<T>, IOutbox<T> where T : notnull
{
    private readonly ConcurrentQueue<T> _queue = new();
    
    public void Enqueue(T item) => _queue.Enqueue(item);
    
    public bool TryDequeue(out T item) => _queue.TryDequeue(out item!);
}

public class Inbox<T> : IInbox<T>, IOutbox<T> where T : notnull
{
    private readonly Queue<T> _queue = new();
    
    public void Enqueue(T item) => _queue.Enqueue(item);
    
    public bool TryDequeue(out T item) => _queue.TryDequeue(out item!);
}