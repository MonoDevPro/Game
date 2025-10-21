using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem(World world, GameEventSystem events) : GameSystem(world, events)
{
    [Query]
    [All<PlayerControlled, Velocity>]
    private void ProcessStartMovement(in Entity e,
        ref Velocity velocity, in Walkable speed, in PlayerInput input, [Data] float _)
    {
        var (nx, ny) = NormalizeInput(input.InputX, input.InputY);
        velocity.DirectionX = nx;
        velocity.DirectionY = ny;
        velocity.Speed = ComputeCellsPerSecond(in speed, input.Flags);
    }

    [Query]
    [All<PlayerControlled, Facing>]
    private void ProcessEntityFacing(in Entity e, in Velocity velocity, ref Facing facing, [Data] float _)
    {
        if (velocity.DirectionX == 0 && velocity.DirectionY == 0) return;
        int previousX = facing.DirectionX;
        int previousY = facing.DirectionY;

        facing.DirectionX = velocity.DirectionX;
        facing.DirectionY = velocity.DirectionY;

        if (previousX != facing.DirectionX || previousY != facing.DirectionY)
        {
            Events.RaiseFacingChanged(e, facing.DirectionX, facing.DirectionY);
        }
    }

    [Query]
    [All<Position, Movement, Velocity, Walkable>]
    private void ProcessMovement(in Entity e, ref Position pos, ref Movement movement, in Velocity velocity, [Data] float deltaTime)
    {
        int previousX = pos.X;
        int previousY = pos.Y;
        int previousZ = pos.Z;

        Step(ref pos, ref movement, in velocity, deltaTime);

        if (previousX != pos.X || previousY != pos.Y || previousZ != pos.Z)
        {
            Events.RaisePositionChanged(e, pos.X, pos.Y);
        }
    }
    
    public (sbyte x, sbyte y) NormalizeInput(sbyte inputX, sbyte inputY)
    {
        sbyte nx = inputX switch { < 0 => -1, > 0 => 1, _ => 0 };
        sbyte ny = inputY switch { < 0 => -1, > 0 => 1, _ => 0 };
        return (nx, ny);
    }
    
    public float ComputeCellsPerSecond(in Walkable walkable, in InputFlags flags)
    {
        float speed = walkable.BaseSpeed + walkable.CurrentModifier;
        if (flags.HasFlag(InputFlags.Sprint))
            speed *= 1.5f;
        return speed;
    }
    
    public bool Step(ref Position pos, ref Movement movement, in Velocity vel, float dt)
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