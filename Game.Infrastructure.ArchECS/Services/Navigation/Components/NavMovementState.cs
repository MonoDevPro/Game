using System.Runtime.CompilerServices;

namespace Game.Infrastructure.ArchECS.Services.Navigation.Components;

/// <summary>
/// Estado de movimento no SERVIDOR - baseado em ticks, não tempo. 
/// </summary>
public struct NavMovementState
{
    public Position TargetCell;     // Próxima célula
    public long StartTick;          // Tick quando começou a mover
    public long EndTick;            // Tick quando deve chegar
    public bool IsMoving;
    public Direction MovementDirection;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ShouldComplete(long currentTick) 
        => IsMoving && currentTick >= EndTick;

    public void StartMove(Position from, Position to, long currentTick, int durationTicks)
    {
        TargetCell = to;
        StartTick = currentTick;
        EndTick = currentTick + durationTicks;
        IsMoving = true;
        MovementDirection = GetDirection(from, to);
    }

    public void Complete()
    {
        IsMoving = false;
    }

    public void Reset()
    {
        IsMoving = false;
    }

    private static Direction GetDirection(Position from, Position to)
    {
        return new Direction
        {
            X = Math.Sign(to.X - from.X),
            Y = Math.Sign(to.Y - from.Y)
        };
    }
}