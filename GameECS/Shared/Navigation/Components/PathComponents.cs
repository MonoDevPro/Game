using System.Runtime.CompilerServices;

namespace GameECS.Shared.Navigation.Components;

#region Path Request

/// <summary>
/// Requisição de cálculo de caminho.
/// </summary>
public struct PathRequest
{
    public int TargetX;
    public int TargetY;
    public PathRequestFlags Flags;
    public PathPriority Priority;

    public static PathRequest Create(int targetX, int targetY, 
        PathPriority priority = PathPriority.Normal,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        return new PathRequest
        {
            TargetX = targetX,
            TargetY = targetY,
            Priority = priority,
            Flags = flags
        };
    }

    public static PathRequest Create(GridPosition target,
        PathPriority priority = PathPriority.Normal,
        PathRequestFlags flags = PathRequestFlags.None)
        => Create(target.X, target.Y, priority, flags);
}

[Flags]
public enum PathRequestFlags : byte
{
    None = 0,
    AllowPartialPath = 1 << 0,
    IgnoreDynamicObstacles = 1 << 1,
    CardinalOnly = 1 << 2,
    Revalidate = 1 << 3
}

public enum PathPriority : byte
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

#endregion

#region Path State

/// <summary>
/// Estado do pathfinding de uma entidade.
/// </summary>
public struct PathState
{
    public PathStatus Status;
    public PathFailReason FailReason;
    public byte AttemptCount;
    public long LastUpdateTick;

    public readonly bool IsActive => Status is PathStatus.Pending 
        or PathStatus.Computing or PathStatus.Ready or PathStatus.Following;

    public readonly bool HasFailed => Status == PathStatus.Failed;
    public readonly bool IsComplete => Status == PathStatus.Completed;
}

public enum PathStatus : byte
{
    None = 0,
    Pending,
    Computing,
    Ready,
    Following,
    Completed,
    Failed,
    Cancelled
}

public enum PathFailReason : byte
{
    None = 0,
    NoPathExists,
    StartBlocked,
    GoalBlocked,
    Timeout,
    TooFarAway,
    InvalidRequest,
    AlreadyAtGoal,
    BufferTooSmall
}

#endregion

#region Path Buffer

/// <summary>
/// Buffer de caminho com waypoints (índices do grid).
/// </summary>
public struct GridPathBuffer
{
    public const int MaxWaypoints = 128;

    public unsafe fixed int WaypointIndices[MaxWaypoints];

    public int WaypointCount;
    public int CurrentIndex;
    public int GoalX;
    public int GoalY;

    public readonly bool IsValid => WaypointCount > 0;
    public readonly bool IsComplete => CurrentIndex >= WaypointCount;
    public readonly int RemainingCount => Math.Max(0, WaypointCount - CurrentIndex);
    public readonly GridPosition Goal => new(GoalX, GoalY);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetWaypoint(int index, int gridIndex)
    {
        if ((uint)index < MaxWaypoints)
            WaypointIndices[index] = gridIndex;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe int GetWaypoint(int index)
    {
        if ((uint)index < (uint)WaypointCount)
            return WaypointIndices[index];
        return -1;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int GetCurrentWaypoint() => GetWaypoint(CurrentIndex);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public GridPosition GetCurrentWaypointPosition(int gridWidth)
    {
        int idx = GetCurrentWaypoint();
        if (idx < 0) return GridPosition.Invalid;
        return new GridPosition(idx % gridWidth, idx / gridWidth);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool TryAdvance()
    {
        if (CurrentIndex < WaypointCount)
        {
            CurrentIndex++;
            return true;
        }
        return false;
    }

    public void Clear()
    {
        WaypointCount = 0;
        CurrentIndex = 0;
        GoalX = 0;
        GoalY = 0;
    }

    public void SetGoal(GridPosition goal)
    {
        GoalX = goal.X;
        GoalY = goal.Y;
    }
}

#endregion

#region Tags

/// <summary>
/// Tag: entidade tem capacidade de navegação.
/// </summary>
public struct NavigationAgent { }

/// <summary>
/// Tag: entidade está em movimento.
/// </summary>
public struct IsMoving { }

/// <summary>
/// Tag: entidade chegou ao destino.
/// </summary>
public struct ReachedDestination { }

/// <summary>
/// Tag: entidade bloqueada aguardando.
/// </summary>
public struct WaitingForPath
{
    public long StartTick;
    public int BlockerId;
}

#endregion