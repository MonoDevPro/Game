namespace Game.ECS.Components;

public enum MovementResult
{
    None,
    OutOfBounds,
    BlockedByMap,
    BlockedByEntity,
    Allowed
}

// ============================================
// Transform - Posicionamento
// ============================================
public struct MapId                             { public int Value; }
public struct Speed                             { public float Value; }
public struct Direction                         { public sbyte X; public sbyte Y; }
public struct Position : IEquatable<Position>   { 
    public int X; 
    public int Y;
    public int Z;
    
    public bool Equals(Position other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is Position other && Equals(other); 
    public override int GetHashCode() => HashCode.Combine(X, Y, Z); }

/// <summary>
/// Rastreia a célula registrada no índice espacial para manter o mapa consistente.
/// </summary>
public struct SpatialAnchor
{
    public int MapId;
    public Position Position;
    public bool IsTracked;
}