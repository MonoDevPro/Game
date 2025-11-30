namespace Game.ECS.Components;

// ============================================
// Transform - Posicionamento
// ============================================
public struct MapId                             { public int Value; }
public struct Floor                             { public sbyte Value; }
public struct Speed                             { public float Value; }
public struct Direction                         { public sbyte X; public sbyte Y; }
public struct Position : IEquatable<Position>   { public int X; public int Y;
    
    public bool Equals(Position other) => X == other.X && Y == other.Y; 
    public override bool Equals(object? obj) => obj is Position other && Equals(other); 
    public override int GetHashCode() => HashCode.Combine(X, Y); }
    
// ============================================
// Movement - Movimento
// ============================================
public struct Walkable
{
    public float BaseSpeed; 
    public float CurrentModifier; 
    public float Accumulator;
}