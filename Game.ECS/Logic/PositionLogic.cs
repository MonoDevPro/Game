using Game.ECS.Components;

namespace Game.ECS.Logic;

public static class PositionLogic
{

    public static (sbyte x, sbyte y) GetDirectionTowards(in Position from, in Position to)
    {
        int deltaX = to.X - from.X;
        int deltaY = to.Y - from.Y;
        sbyte directionX = (sbyte)Math.Sign(deltaX);
        sbyte directionY = (sbyte)Math.Sign(deltaY);
        return (directionX, directionY);
    }
}