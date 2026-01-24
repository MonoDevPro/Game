namespace Game.Infrastructure.ArchECS.Commons.Components;

// Component Definitions - simple structs representing data components

// Character-related components 
public struct CharacterId               { public int Value; }
public struct AccountId                 { public int Value; }

// Spatial components
public struct MapId                     { public int Value; }
public struct FloorId                   { public int Value; }
public struct Direction                 { public int X; public int Y; }
public struct Velocity                  { public int X; public int Y; }
public struct Position : IEquatable<Position> { public int X; public int Y;
    public bool Equals(Position other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Position other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}