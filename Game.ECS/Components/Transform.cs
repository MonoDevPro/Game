// ============================================
// Transform - Posicionamento
// ============================================

namespace Game.ECS.Components;

public struct Position(int x, int y, int z) : IEquatable<Position>
{ public int X = x; public int Y = y; public int Z = z;

    /// <summary>
    /// Distância Manhattan (taxicab) em células.
    /// </summary>
    public readonly int ManhattanDistance(Position other)
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    public bool Equals(Position other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is Position other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
}

public struct Velocity
{
    public int DirectionX; 
    public int DirectionY; 
    public float Speed;
    
    public bool IsMoving() => Speed > 0f && (DirectionX != 0 || DirectionY != 0);
    public void Stop() { Speed = 0f; DirectionX = 0; DirectionY = 0; }
}
