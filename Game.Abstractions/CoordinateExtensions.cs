using Game.Domain.VOs;

namespace Game.Abstractions;

public static class CoordinateExtensions
{
    public static Coordinate ToSignedCoordinate(this GridOffset offset) 
        => new(Math.Sign(offset.X), Math.Sign(offset.Y));
    
    /// <summary>Distância euclidiana (double).</summary>
    public static double DistanceTo(this Coordinate current, Coordinate other)
    {
        var dx = other.X - current.X;
        var dy = other.Y - current.Y;
        return Math.Sqrt(dx * dx + dy * dy);
    }

    /// <summary>Distância euclidiana ao quadrado — evita Math.Sqrt quando só fazemos comparações.</summary>
    public static int DistanceSquaredTo(this Coordinate current, Coordinate other)
    {
        var dx = other.X - current.X;
        var dy = other.Y - current.Y;
        return dx * dx + dy * dy;
    }

    /// <summary>Manhattan distance (útil para grids).</summary>
    public static int ManhattanDistanceTo(this Coordinate current, Coordinate other) 
        => Math.Abs(other.X - current.X) + Math.Abs(other.Y - current.Y);

    /// <summary>Retorna true se for vizinho 4-direções (N,S,E,W).</summary>
    public static bool IsAdjacent4(this Coordinate current, Coordinate other) 
        => current.DistanceSquaredTo(other) == 1;

    /// <summary>Retorna true se for vizinho nas 8 direções (inclui diagonais).</summary>
    public static bool IsAdjacent8(this Coordinate current, Coordinate other)
        => Math.Max(Math.Abs(other.X - current.X), Math.Abs(other.Y - current.Y)) == 1;

    public static Coordinate Add(this Coordinate current, int dx, int dy) 
        => new(current.X + dx, current.Y + dy);
    public static Coordinate Add(this Coordinate current, Coordinate other) 
        => new(current.X + other.X, current.Y + other.Y);
    public static Coordinate Sub(this Coordinate current, Coordinate other)
        => new(current.X - other.X, current.Y - other.Y);
    public static Coordinate Div(this Coordinate current, Coordinate other)
        => new(current.X - other.X, current.Y - other.Y);
    
    public static Coordinate Clamp(this Coordinate current, int minX, int minY, int maxX, int maxY)
        => new(Math.Clamp(current.X, minX, maxX), Math.Clamp(current.Y, minY, maxY));
}