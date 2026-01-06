using System.Runtime.CompilerServices;

namespace Game.ECS.Navigation.Components;

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

public struct PathBuffer
{
    public const int MaxWaypoints = 128;

    // Buffer inline de índices de grid
    public unsafe fixed int WaypointIndices[MaxWaypoints];

    public int WaypointCount;
    public int CurrentIndex;
    public int GoalX;
    public int GoalY;

    public readonly bool IsValid => WaypointCount > 0;
    public readonly bool IsComplete => CurrentIndex >= WaypointCount;
    public readonly int RemainingWaypoints => WaypointCount - CurrentIndex;

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public unsafe void SetWaypoint(int index, int gridIndex)
    {
        if (index >= 0 && index < MaxWaypoints)
        {
            WaypointIndices[index] = gridIndex;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe int GetWaypoint(int index)
    {
        if (index >= 0 && index < WaypointCount)
        {
            return WaypointIndices[index];
        }
        return -1;
    }

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public int GetCurrentWaypoint() => GetWaypoint(CurrentIndex);

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public int GetNextWaypoint() => GetWaypoint(CurrentIndex + 1);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool AdvanceWaypoint()
    {
        if (CurrentIndex < WaypointCount)
        {
            CurrentIndex++;
            return true;
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridPosition GetCurrentWaypointAsPosition(int gridWidth)
    {
        int idx = GetCurrentWaypoint();
        if (idx < 0) return new GridPosition(-1, -1);
        return new GridPosition(idx % gridWidth, idx / gridWidth);
    }

    public void Clear()
    {
        WaypointCount = 0;
        CurrentIndex = 0;
        GoalX = 0;
        GoalY = 0;
    }
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
