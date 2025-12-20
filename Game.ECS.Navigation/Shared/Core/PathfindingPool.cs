using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace Game.ECS.Navigation.Shared.Core;

/// <summary>
/// Pool lock-free de contextos para pathfinding. 
/// </summary>
public sealed class PathfindingPool
{
    private readonly ConcurrentBag<PathfindingContext> _contextPool;
    private readonly int _nodeCapacity;
    private readonly int _pathCapacity;

    public PathfindingPool(int nodeCapacity = 4096, int pathCapacity = 256, int preWarmCount = 8)
    {
        _nodeCapacity = nodeCapacity;
        _pathCapacity = pathCapacity;
        _contextPool = new ConcurrentBag<PathfindingContext>();

        for (int i = 0; i < preWarmCount; i++)
        {
            _contextPool.Add(CreateNewContext());
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PathfindingContext Rent()
    {
        if (_contextPool.TryTake(out var context))
        {
            context.Reset();
            return context;
        }
        return CreateNewContext();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Return(PathfindingContext context)
    {
        _contextPool.Add(context);
    }

    private PathfindingContext CreateNewContext()
    {
        return new PathfindingContext(_nodeCapacity, _pathCapacity);
    }
}