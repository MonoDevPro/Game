using Game.ECS.Components;

namespace Game.ECS.Logic;

public static partial class PositionLogic
{
    public static (sbyte X, sbyte Y) GetDirectionTowards(in Position from, in Position to) 
        => ((sbyte)Math.Sign(to.X - from.X), (sbyte)Math.Sign(to.Y - from.Y));
    
    public static float CalculateDistance(in Position a, in Position b)
    {
        float deltaX = b.X - a.X;
        float deltaY = b.Y - a.Y;
        return MathF.Sqrt(deltaX * deltaX + deltaY * deltaY);
    }
    
}