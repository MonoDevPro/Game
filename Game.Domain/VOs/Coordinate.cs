using MemoryPack;

namespace Game.Domain.VOs;

// Estrutura auxiliar para representar coordenadas 2D
public readonly record struct Coordinate(int X, int Y)
{
    public static readonly Coordinate Zero = new(0, 0);
    
    public static Coordinate operator +(Coordinate a, Coordinate b) => new(a.X + b.X, a.Y + b.Y);
    public static Coordinate operator -(Coordinate a, Coordinate b) => new(a.X - b.X, a.Y - b.Y);
    public static Coordinate operator *(Coordinate a, int scalar) => new(a.X * scalar, a.Y * scalar);
    public static Coordinate operator /(Coordinate a, int scalar) => new(a.X / scalar, a.Y / scalar);
    
    public override string ToString() => $"({X}, {Y})";
}