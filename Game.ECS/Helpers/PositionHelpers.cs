using Game.ECS.Components;

namespace Game.ECS.Helpers;

public static class PositionHelpers
{
    public static (sbyte X, sbyte Y) GetDirectionTowards(in Position from, in Position to)
    {
        return ((sbyte)Math.Sign(to.X - from.X), (sbyte)Math.Sign(to.Y - from.Y));
    }

    public static float CalculateDistance(in Position a, in Position b)
    {
        float deltaX = b.X - a.X;
        float deltaY = b.Y - a.Y;
        return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
    
    public static int ManhattanDistance(Position pos, Position other)
    {
        return Math.Abs(pos.X - other.X) + Math.Abs(pos.Y - other.Y);
    }

    public static int EuclideanDistanceSquared(Position pos, Position other)
    {
        return (pos.X - other.X) * (pos.X - other.X) + (pos.Y - other.Y) * (pos.Y - other.Y);
    }
}