using System.Runtime. CompilerServices;
using System.Runtime.InteropServices;
using Game.ECS.Components;

namespace Game.ECS.Services.Pathfinding;

public enum PathfindingStatus : byte { Pending, InProgress, Completed, Failed, Cancelled }

public struct PathfindingRequest
{
    public int StartX;
    public int StartY;
    public int GoalX;
    public int GoalY;
    public int MaxSearchNodes;
    public PathfindingStatus Status;
}

/// <summary>
/// Nó do A* com generation counter para invalidação rápida
/// </summary>
public struct PathNode :  IEquatable<PathNode>
{
    public int X;
    public int Y;
    public float GCost;
    public float HCost;
    public int ParentIndex;
    public int Generation;  // ← NOVO: Marca qual "sessão" de busca este nó pertence

    public float FCost => GCost + HCost;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidForGeneration(int currentGeneration) => Generation == currentGeneration;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(PathNode other) => X == other.X && Y == other.Y;

    public override int GetHashCode() => X * 31 + Y;
}

public struct PathResult
{
    public int PathLength;
    public bool IsValid;
}
