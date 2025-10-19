using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem(World world) : GameSystem(world)
{
    [Query]
    [All<PlayerControlled, Velocity>]
    private void ProcessStartMovement(in Entity e,
        ref Velocity velocity, in Walkable speed, in PlayerInput input, [Data] float _)
    {
        var (nx, ny) = MovementMath.NormalizeInput(input.InputX, input.InputY);
        velocity.DirectionX = nx;
        velocity.DirectionY = ny;
        velocity.Speed = MovementMath.ComputeCellsPerSecond(in speed, in input.Flags);
    }

    [Query]
    [All<PlayerControlled, Facing>]
    private void ProcessEntityFacing(in Entity e, in Velocity velocity, ref Facing facing, [Data] float _)
    {
        if (velocity.DirectionX == 0 && velocity.DirectionY == 0) return;
        facing.DirectionX = velocity.DirectionX;
        facing.DirectionY = velocity.DirectionY;
    }

    [Query]
    [All<Position, Movement, Velocity, Walkable>]
    private void ProcessMovement(in Entity e, ref Position pos, ref Movement movement, in Velocity velocity, [Data] float deltaTime)
    {
        MovementMath.Step(ref pos, ref movement, in velocity, deltaTime);
    }
}