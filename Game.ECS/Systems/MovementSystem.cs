using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem(World world, GameEventSystem events, EntityFactory factory) : GameSystem(world, events, factory)
{
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
            Events.RaiseFacingChanged(e, facing.DirectionX, facing.DirectionY);
    }

    [Query]
    [All<Position, Movement, Velocity, Walkable>]
    private void ProcessMovement(in Entity e, ref Position pos, ref Movement movement, ref Velocity velocity, [Data] float deltaTime)
    {
        if (Step(ref pos, ref movement, ref velocity, deltaTime))
            Events.RaisePositionChanged(e, pos.X, pos.Y);
    }
    
    private bool Step(ref Position pos, ref Movement movement, ref Velocity vel, float dt)
    {
        if ((vel.DirectionX == 0 && vel.DirectionY == 0) || vel.Speed <= 0f)
            return false;

        movement.Timer += vel.Speed * dt;
        if (movement.Timer < SimulationConfig.CellSize)
            return false;

        movement.Timer -= SimulationConfig.CellSize;
        pos.X += vel.DirectionX;
        pos.Y += vel.DirectionY;
        vel.Speed = 0f; // Para parar o movimento até o próximo input
        return true;
    }
}