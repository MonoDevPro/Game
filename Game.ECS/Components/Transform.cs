// ============================================
// Transform - Posicionamento
// ============================================

namespace Game.ECS.Components;

public struct Floor { public sbyte Level; }
public struct Speed { public float Value; }

public struct Position : IEquatable<Position> { 
    public int X; 
    public int Y;
    
    public bool Equals(Position other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Position other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}