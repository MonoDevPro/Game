using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;

namespace Game.ECS.Systems;

public sealed partial class PlayerInputSystem(World world) : GameSystem(world)
{
    [Query]
    [All<PlayerInput, Velocity, MovementSpeed, PlayerControlled>]
    [None<Dead>]
    private void ProcessInput([Data]in float deltaTime, ref PlayerInput input, ref Velocity vel, in MovementSpeed speed)
    {
        float actualSpeed = speed.BaseSpeed * speed.CurrentModifier;
        
        if ((input.Flags & InputFlags.Sprint) != 0)
            actualSpeed *= 1.5f;
        
        vel.Value = new Coordinate(
            X: (int)(input.Movement.X * actualSpeed), 
            Y: (int)(input.Movement.Y * actualSpeed));
    }
}