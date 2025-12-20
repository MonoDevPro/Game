using System.Runtime.CompilerServices;
using Game.ECS.Navigation.Shared.Components;

namespace Game.ECS.Navigation.Server.Components;

/// <summary>
/// Estado de movimento no servidor (tick-based).
/// </summary>
public struct ServerMovement
{
    public GridPosition TargetCell;
    public long StartTick;
    public long EndTick;
    public bool IsMoving;
    public MovementDirection Direction;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool ShouldComplete(long currentTick) 
        => IsMoving && currentTick >= EndTick;

    public void Start(GridPosition from, GridPosition to, long currentTick, int durationTicks)
    {
        TargetCell = to;
        StartTick = currentTick;
        EndTick = currentTick + durationTicks;
        IsMoving = true;
        Direction = MovementDirectionExtensions.FromPositions(from, to);
    }

    public void Complete() => IsMoving = false;
    public void Reset() => IsMoving = false;
}

/// <summary>
/// Configuração de agente no servidor.
/// </summary>
public struct ServerAgentConfig
{
    public int CardinalMoveTicks;
    public int DiagonalMoveTicks;
    public bool AllowDiagonal;
    public byte MaxRetries;

    public static ServerAgentConfig Default => new()
    {
        CardinalMoveTicks = 6,   // ~100ms @ 60 ticks/s
        DiagonalMoveTicks = 9,   // ~150ms
        AllowDiagonal = true,
        MaxRetries = 3
    };

    public static ServerAgentConfig Slow => new()
    {
        CardinalMoveTicks = 12,
        DiagonalMoveTicks = 17,
        AllowDiagonal = true,
        MaxRetries = 3
    };

    public static ServerAgentConfig Fast => new()
    {
        CardinalMoveTicks = 3,
        DiagonalMoveTicks = 4,
        AllowDiagonal = true,
        MaxRetries = 3
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMoveTicks(bool diagonal) 
        => diagonal ? DiagonalMoveTicks : CardinalMoveTicks;
}