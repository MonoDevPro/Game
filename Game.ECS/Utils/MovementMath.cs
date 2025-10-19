using Game.ECS.Components;

namespace Game.ECS.Utils;

public static class MovementMath
{
    public static (sbyte x, sbyte y) NormalizeInput(sbyte inputX, sbyte inputY)
    {
        sbyte nx = inputX switch { < 0 => -1, > 0 => 1, _ => 0 };
        sbyte ny = inputY switch { < 0 => -1, > 0 => 1, _ => 0 };
        return (nx, ny);
    }

    public static float ComputeCellsPerSecond(in Walkable walkable, in InputFlags flags)
    {
        float speed = walkable.BaseSpeed + walkable.CurrentModifier;
        if (flags.HasFlag(InputFlags.Sprint))
            speed *= 1.5f;
        return speed;
    }

    // Step determinístico por tick
    public static bool Step(ref Position pos, ref Movement movement, in Velocity vel, float dt)
    {
        if ((vel.DirectionX == 0 && vel.DirectionY == 0) || vel.Speed <= 0f)
            return false;

        movement.Timer += vel.Speed * dt;
        if (movement.Timer < SimulationConfig.CellSize)
            return false;

        movement.Timer -= SimulationConfig.CellSize;
        pos.X += vel.DirectionX;
        pos.Y += vel.DirectionY;
        return true;
    }
}