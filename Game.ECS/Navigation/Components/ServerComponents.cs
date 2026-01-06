using System.Runtime.CompilerServices;
using Game.Domain.Enums;

namespace Game.ECS. Navigation. Components;

// ============================================
// COMPONENTES DO SERVIDOR (Autoritativos)
// ============================================

/// <summary>
/// Posição no grid - SERVIDOR (fonte de verdade).
/// </summary>
public struct GridPosition :  IEquatable<GridPosition>
{
    public int X;
    public int Y;

    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public readonly int ManhattanDistanceTo(GridPosition other)
        => Math.Abs(X - other. X) + Math.Abs(Y - other.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(GridPosition other) 
        => X == other.X && Y == other.Y;

    public readonly override bool Equals(object? obj) 
        => obj is GridPosition other && Equals(other);

    public readonly override int GetHashCode() 
        => HashCode.Combine(X, Y);

    public static bool operator ==(GridPosition left, GridPosition right) 
        => left.Equals(right);

    public static bool operator !=(GridPosition left, GridPosition right) 
        => !left.Equals(right);

    public readonly override string ToString() => $"({X}, {Y})";
}

/// <summary>
/// Estado de movimento no SERVIDOR - baseado em ticks, não tempo. 
/// </summary>
public struct MovementState
{
    public GridPosition TargetCell;     // Próxima célula
    public long StartTick;              // Tick quando começou a mover
    public long EndTick;                // Tick quando deve chegar
    public bool IsMoving;
    public DirectionEnum Direction;

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public readonly bool ShouldComplete(long currentTick) 
        => IsMoving && currentTick >= EndTick;

    public void StartMove(GridPosition from, GridPosition to, long currentTick, int durationTicks)
    {
        TargetCell = to;
        StartTick = currentTick;
        EndTick = currentTick + durationTicks;
        IsMoving = true;
        Direction = GetDirection(from, to);
    }

    public void Complete()
    {
        IsMoving = false;
    }

    public void Reset()
    {
        IsMoving = false;
    }

    private static DirectionEnum GetDirection(GridPosition from, GridPosition to)
    {
        int dx = Math.Sign(to.X - from.X);
        int dy = Math.Sign(to. Y - from.Y);

        return (dx, dy) switch
        {
            (0, -1) => DirectionEnum.North,
            (1, -1) => DirectionEnum.NorthEast,
            (1, 0) => DirectionEnum.East,
            (1, 1) => DirectionEnum.SouthEast,
            (0, 1) => DirectionEnum.South,
            (-1, 1) => DirectionEnum.SouthWest,
            (-1, 0) => DirectionEnum. West,
            (-1, -1) => DirectionEnum. NorthWest,
            _ => DirectionEnum.None
        };
    }
}

/// <summary>
/// Configuração do agente - SERVIDOR.
/// Usa ticks ao invés de tempo. 
/// </summary>
public struct ServerAgentSettings
{
    public int MoveDurationTicks;        // Ticks para mover 1 célula
    public int DiagonalDurationTicks;    // Ticks para diagonal
    public bool AllowDiagonal;
    public byte MaxPathRetries;

    public static ServerAgentSettings Default => new()
    {
        MoveDurationTicks = 6,           // ~100ms a 60 ticks/s
        DiagonalDurationTicks = 9,       // ~141ms
        AllowDiagonal = true,
        MaxPathRetries = 3
    };

    public static ServerAgentSettings Slow => new()
    {
        MoveDurationTicks = 12,
        DiagonalDurationTicks = 17,
        AllowDiagonal = true,
        MaxPathRetries = 3
    };

    public static ServerAgentSettings Fast => new()
    {
        MoveDurationTicks = 3,
        DiagonalDurationTicks = 4,
        AllowDiagonal = true,
        MaxPathRetries = 3
    };

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public readonly int GetDuration(bool isDiagonal)
        => isDiagonal ?  DiagonalDurationTicks : MoveDurationTicks;
}

/// <summary>
/// Tag:  entidade é um agente de navegação.
/// </summary>
public struct GridNavigationAgent { }

/// <summary>
/// Tag:  entidade está se movendo.
/// </summary>
public struct IsMoving { }

/// <summary>
/// Tag: entidade chegou ao destino. 
/// </summary>
public struct ReachedDestination { }

/// <summary>
/// Tag: entidade bloqueada aguardando. 
/// </summary>
public struct WaitingToMove
{
    public long WaitTime;
    public int BlockedByEntityId;
}