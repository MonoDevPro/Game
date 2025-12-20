using System.Runtime.CompilerServices;

namespace Game.ECS.Navigation.Components;

/// <summary>
/// Estado de movimento entre células do grid.
/// </summary>
public struct GridMovement
{
    public GridPosition From;           // Célula de origem
    public GridPosition To;             // Célula de destino
    public float Progress;              // 0.0 a 1.0
    public float Duration;              // Tempo total para mover uma célula
    public bool IsMoving;               // Está em movimento? 
    public MovementDirection Direction; // Direção atual

    public readonly bool IsComplete => Progress >= 1.0f;

    public void StartMove(GridPosition from, GridPosition to, float duration)
    {
        From = from;
        To = to;
        Progress = 0f;
        Duration = duration;
        IsMoving = true;
        Direction = GetDirection(from, to);
    }

    public void Complete()
    {
        Progress = 1f;
        IsMoving = false;
    }

    public void Reset()
    {
        IsMoving = false;
        Progress = 0f;
    }

    private static MovementDirection GetDirection(GridPosition from, GridPosition to)
    {
        int dx = to.X - from.X;
        int dy = to.Y - from. Y;

        return (dx, dy) switch
        {
            (0, -1) => MovementDirection.North,
            (1, -1) => MovementDirection.NorthEast,
            (1, 0) => MovementDirection.East,
            (1, 1) => MovementDirection.SouthEast,
            (0, 1) => MovementDirection.South,
            (-1, 1) => MovementDirection.SouthWest,
            (-1, 0) => MovementDirection. West,
            (-1, -1) => MovementDirection. NorthWest,
            _ => MovementDirection.None
        };
    }
}

/// <summary>
/// Configurações de movimento no grid.
/// </summary>
public struct GridMovementSettings
{
    public float MoveSpeed;              // Células por segundo
    public bool AllowDiagonal;           // Permite movimento diagonal? 
    public bool SmoothMovement;          // Interpola visualmente?
    public float DiagonalSpeedMultiplier; // Multiplicador para diagonal (geralmente 1/sqrt(2))

    public static GridMovementSettings Default => new()
    {
        MoveSpeed = 5f,
        AllowDiagonal = true,
        SmoothMovement = true,
        DiagonalSpeedMultiplier = 0.707f // 1/sqrt(2)
    };

    public static GridMovementSettings Cardinal => new()
    {
        MoveSpeed = 5f,
        AllowDiagonal = false,
        SmoothMovement = true,
        DiagonalSpeedMultiplier = 1f
    };

    public static GridMovementSettings Fast => new()
    {
        MoveSpeed = 10f,
        AllowDiagonal = true,
        SmoothMovement = true,
        DiagonalSpeedMultiplier = 0.707f
    };

    /// <summary>
    /// Retorna duração do movimento para uma célula. 
    /// </summary>
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public readonly float GetMoveDuration(bool isDiagonal)
    {
        float speed = MoveSpeed;
        if (isDiagonal)
        {
            speed *= DiagonalSpeedMultiplier;
        }
        return 1f / speed;
    }
}

/// <summary>
/// Facing direction (para sprites/animação).
/// </summary>
public struct Facing
{
    public MovementDirection Direction;

    public Facing(MovementDirection direction)
    {
        Direction = direction;
    }
}