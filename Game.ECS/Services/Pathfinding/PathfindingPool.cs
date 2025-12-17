using System.Buffers;
using System.Collections.Concurrent;
using System. Runtime.CompilerServices;

namespace Game.ECS.Services. Pathfinding;

/// <summary>
/// Pool lock-free de contextos para pathfinding
/// </summary>
public sealed class PathfindingPool
{
    private readonly ArrayPool<PathNode> _nodePool;
    private readonly ArrayPool<int> _pathPool;
    
    // ConcurrentBag é lock-free para a maioria das operações
    private readonly ConcurrentBag<PathfindingContext> _contextPool;

    private readonly int _defaultNodeArraySize;
    private readonly int _defaultPathArraySize;

    public PathfindingPool(int defaultNodeArraySize = 1024, int defaultPathArraySize = 256, int preWarmCount = 8)
    {
        _defaultNodeArraySize = defaultNodeArraySize;
        _defaultPathArraySize = defaultPathArraySize;
        
        _nodePool = ArrayPool<PathNode>.Shared;
        _pathPool = ArrayPool<int>. Shared;
        _contextPool = new ConcurrentBag<PathfindingContext>();

        // Pré-aquece o pool
        for (int i = 0; i < preWarmCount; i++)
        {
            _contextPool.Add(CreateNewContext());
        }
    }

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public PathfindingContext RentContext()
    {
        return _contextPool.TryTake(out var context) 
            ? context 
            :  CreateNewContext();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ReturnContext(PathfindingContext context)
    {
        context.Reset();
        _contextPool.Add(context);
    }

    private PathfindingContext CreateNewContext()
    {
        return new PathfindingContext(
            _nodePool.Rent(_defaultNodeArraySize),
            _pathPool.Rent(_defaultPathArraySize),
            _defaultNodeArraySize,
            _defaultPathArraySize
        );
    }
}