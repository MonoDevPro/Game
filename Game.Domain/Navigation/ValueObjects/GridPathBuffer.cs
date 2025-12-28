using System.Runtime.CompilerServices;
using Game.Domain.ValueObjects.Map;

namespace Game.Domain.Navigation.ValueObjects;

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