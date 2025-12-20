
using Game.ECS.Navigation.Components;

namespace Game.ECS.Navigation.Data;

/// <summary>
/// Resultado de uma operação de pathfinding.
/// </summary>
public readonly struct PathResult
{
    public bool Success { get; init; }
    public int PathLength { get; init; }
    public int NodesSearched { get; init; }
    public float ComputeTimeMs { get; init; }
    public PathFailReason FailReason { get; init; }
    
    public static PathResult Succeeded(int pathLength, int nodesSearched, float computeTime) => new()
    {
        Success = true,
        PathLength = pathLength,
        NodesSearched = nodesSearched,
        ComputeTimeMs = computeTime,
        FailReason = PathFailReason.None
    };

    public static PathResult Failed(PathFailReason reason, int nodesSearched = 0, float computeTime = 0) => new()
    {
        Success = false,
        PathLength = 0,
        NodesSearched = nodesSearched,
        ComputeTimeMs = computeTime,
        FailReason = reason
    };
}