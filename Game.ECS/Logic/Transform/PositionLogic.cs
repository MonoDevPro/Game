using Game.ECS.Components;

namespace Game.ECS.Logic;

public static partial class PositionLogic
{
    public static Facing GetDirectionTowards(in Position from, in Position to) 
        => new() { DirectionX = (sbyte)Math.Sign(to.X - from.X), DirectionY = (sbyte)Math.Sign(to.Y - from.Y) };
}