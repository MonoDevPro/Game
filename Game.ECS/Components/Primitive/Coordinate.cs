using System.Runtime.InteropServices;

namespace Game.ECS.Components.Primitive;

// Estrutura auxiliar para representar coordenadas 2D
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Coordinate(int X, int Y)
{
    public static Coordinate operator +(Coordinate a, Coordinate b) => new(a.X + b.X, a.Y + b.Y);
    public static Coordinate operator -(Coordinate a, Coordinate b) => new(a.X - b.X, a.Y - b.Y);
    public static Coordinate operator *(Coordinate a, Coordinate b) => new(a.X * b.X, a.Y * b.Y);
    public static Coordinate operator /(Coordinate a, Coordinate b) => new(a.X / b.X, a.Y / b.Y);
    
    public static readonly Coordinate Zero = new(0, 0);
    public Coordinate Signed() => new(Math.Sign(X), Math.Sign(Y));
    public int ManhattanDistance() => Math.Abs(X) + Math.Abs(Y);
    public double EuclideanDistance() => Math.Sqrt(X * X + Y * Y);
    
    public override string ToString() => $"({X}, {Y})";
}