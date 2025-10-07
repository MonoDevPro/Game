namespace Game.ECS.Components;

public struct Position { public Coordinate Value; }
public struct Rotation { public ushort Value; }
public struct Velocity { public Coordinate Value; }
public struct Scale { public int Value; }



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
