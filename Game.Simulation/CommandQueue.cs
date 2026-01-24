using System.Collections.Concurrent;

namespace Game.Simulation;

public sealed class CommandQueue
{
    private readonly ConcurrentQueue<IWorldCommand> _queue = new();

    public void Enqueue(IWorldCommand command)
    {
        _queue.Enqueue(command);
    }

    public int Drain(WorldState state)
    {
        var count = 0;
        while (_queue.TryDequeue(out var command))
        {
            command.Apply(state);
            count++;
        }

        return count;
    }
}
