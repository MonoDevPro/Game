using System.Runtime.CompilerServices;

namespace Game.ECS.Navigation.Components;

#region Request Components

/// <summary>
/// Componente que solicita cálculo de caminho. 
/// Usa coordenadas de GRID (inteiros).
/// </summary>
public struct PathRequest
{
    public int TargetX;
    public int TargetY;
    public PathRequestFlags Flags;
    public PathPriority Priority;
    public int MaxSearchNodes;

    public static PathRequest Create(int targetX, int targetY, PathPriority priority = PathPriority.Normal)
    {
        return new PathRequest
        {
            TargetX = targetX,
            TargetY = targetY,
            Priority = priority,
            Flags = PathRequestFlags.None,
            MaxSearchNodes = 0 // 0 = usar default
        };
    }
}

[Flags]
public enum PathRequestFlags :  byte
{
    None = 0,
    AllowPartialPath = 1 << 0,
    IgnoreDynamicObstacles = 1 << 1,
    CardinalOnly = 1 << 2,  // Apenas movimento cardinal (sem diagonal)
    Revalidate = 1 << 3     // Revalida caminho existente
}

public enum PathPriority : byte
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

#endregion

#region State Components

/// <summary>
/// Estado atual do pathfinding da entidade.
/// </summary>
public struct PathState
{
    public PathStatus Status;
    public float TimeRequested;
    public float TimeCompleted;
    public int AttemptCount;
    public PathFailReason FailReason;
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

#region Path Data Components

/// <summary>
/// Buffer de caminho otimizado para grid.
/// Armazena ÍNDICES do grid, não coordenadas float.
/// </summary>
public struct GridPathBuffer
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

#endregion