using System. Buffers;
using System.Collections.Concurrent;
using Game.ECS.Navigation.Components;

namespace Game.ECS.Services. Pathfinding.Systems;

public sealed class BatchPathfindingSystem(
    PathfindingSystem pathfinder,
    int maxJobsPerTick = 100,
    int parallelWorkers = 4)
{
    private readonly ConcurrentQueue<PathfindingJob> _jobQueue = new();
    private readonly ArrayPool<int> _pathArrayPool = ArrayPool<int>.Shared;

    public void EnqueueRequest(PathfindingJob job)
    {
        _jobQueue.Enqueue(job);
    }

    public void ProcessTick()
    {
        int processed = 0;
        var jobs = new PathfindingJob[Math.Min(maxJobsPerTick, _jobQueue.Count)];

        while (processed < maxJobsPerTick && _jobQueue.TryDequeue(out var job))
        {
            jobs[processed++] = job;
        }

        if (processed == 0) return;

        Parallel.For(0, processed, new ParallelOptions
        {
            MaxDegreeOfParallelism = parallelWorkers
        }, i =>
        {
            ProcessJob(ref jobs[i]);
        });
    }

    private void ProcessJob(ref PathfindingJob job)
    {
        // Usa array do pool ao invés de stackalloc (mais seguro para callbacks)
        var pathBuffer = _pathArrayPool.Rent(256);
        try
        {
            var result = pathfinder.FindPath(ref job.Request, pathBuffer. AsSpan());
            
            // Callback recebe Span - sem alocação! 
            job. Callback?. Invoke(result, new ReadOnlySpan<int>(pathBuffer, 0, result.PathLength));
        }
        finally
        {
            _pathArrayPool. Return(pathBuffer);
        }
    }
}

public struct PathfindingJob
{
    public PathfindingRequest Request;
    public PathfindingCallback?  Callback;
    public int EntityId;
}

// Delegate que aceita Span - zero allocation
public delegate void PathfindingCallback(PathResult result, ReadOnlySpan<int> path);