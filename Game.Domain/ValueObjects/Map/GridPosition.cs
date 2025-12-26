using System.Runtime.CompilerServices;

namespace Game.Domain.ValueObjects.Map;

/// <summary>
/// Posição no grid (coordenadas inteiras).
/// Usado tanto no servidor quanto no cliente. 
/// </summary>
public struct GridPosition(int x, int y) : IEquatable<GridPosition>
{
    public int X = x;
    public int Y = y;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int ManhattanDistanceTo(GridPosition other)
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int ChebyshevDistanceTo(GridPosition other)
        => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double EuclideanDistance(GridPosition other)
    {
        int deltaX = X - other.X;
        int deltaY = Y - other.Y;
        return Math.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool Equals(GridPosition other) 
        => X == other.X && Y == other.Y;

    public readonly override bool Equals(object? obj) 
        => obj is GridPosition other && Equals(other);

    public readonly override int GetHashCode() 
        => HashCode.Combine(X, Y);

    public static bool operator ==(GridPosition left, GridPosition right) 
        => left.Equals(right);

    public static bool operator !=(GridPosition left, GridPosition right) 
        => !left.Equals(right);

    public static GridPosition operator +(GridPosition a, GridPosition b)
        => new(a.X + b.X, a.Y + b.Y);

    public static GridPosition operator -(GridPosition a, GridPosition b)
        => new(a.X - b.X, a.Y - b.Y);

    public readonly override string ToString() => $"({X}, {Y})";
    
    public static readonly GridPosition Zero = new(0, 0);
}