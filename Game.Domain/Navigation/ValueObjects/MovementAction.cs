using System.Runtime.CompilerServices;
using Game.Domain.Enums;
using Game.Domain.Extensions;
using Game.Domain.ValueObjects.Map;

namespace Game.Domain.Navigation.ValueObjects;

/// <summary>
/// Estado de movimento no servidor (tick-based).
/// </summary>
public struct MovementAction
{
    public GridPosition TargetCell;
    public long StartTick;
    public long EndTick;
    public bool IsMoving;
    public DirectionType Direction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ShouldComplete(long currentTick) 
        => IsMoving && currentTick >= EndTick;

    public void Start(GridPosition from, GridPosition to, long currentTick, int durationTicks)
    {
        TargetCell = to;
        StartTick = currentTick;
        EndTick = currentTick + durationTicks;
        IsMoving = true;
        Direction = DirectionExtensions.FromPositions(from, to);
    }

    public void Complete() => IsMoving = false;
    public void Reset() => IsMoving = false;
}