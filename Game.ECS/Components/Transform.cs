// ============================================
// Transform - Posicionamento
// ============================================

namespace Game.ECS.Components;

public readonly record struct Position(int X, int Y);

public readonly record struct SpatialPosition(int X, int Y, sbyte Floor)
{
    /// <summary>
    /// Converts SpatialPosition to Position (drops the Floor component).
    /// </summary>
    public Position ToPosition() => new(X, Y);
}

public struct Floor { public sbyte Level; }
public struct Velocity { public sbyte X; public sbyte Y; public float Speed; }
