using Game.ECS.Navigation.Shared.Components;

namespace Game.ECS.Navigation.Shared.Data;

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

    public static PathResult Succeeded(int pathLength, int nodesSearched, float computeTimeMs = 0) => new()
    {
        Success = true,
        PathLength = pathLength,
        NodesSearched = nodesSearched,
        ComputeTimeMs = computeTimeMs,
        FailReason = PathFailReason.None
    };

    public static PathResult Failed(PathFailReason reason, int nodesSearched = 0, float computeTimeMs = 0) => new()
    {
        Success = false,
        PathLength = 0,
        NodesSearched = nodesSearched,
        ComputeTimeMs = computeTimeMs,
        FailReason = reason
    };
}