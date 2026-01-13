using System.Runtime.CompilerServices;

namespace Game.ECS.Components;

public partial struct Position(int x, int y, int z) : IEquatable<Position>
{
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public static (sbyte X, sbyte Y) GetDirectionTowards(in Position from, in Position to)
        => ((sbyte)Math.Sign(to.X - from.X), (sbyte)Math.Sign(to.Y - from.Y));
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public static int ManhattanDistance(Position pos, Position other)
        => Math.Abs(pos.X - other.X) + Math.Abs(pos.Y - other.Y); 

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public static int EuclideanDistanceSquared(Position pos, Position other)
        => (pos.X - other.X) * (pos.X - other.X) + (pos.Y - other.Y) * (pos.Y - other.Y);
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public float CalculateDistance(Position b)
    {
        float deltaX = b.X - X;
        float deltaY = b.Y - Y;
        return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
    
    public bool Equals(Position other) => X == other.X && Y == other.Y && Z == other.Z;
    public override bool Equals(object? obj) => obj is Position other && Equals(other); 
    public override int GetHashCode() => HashCode.Combine(X, Y, Z); 
}