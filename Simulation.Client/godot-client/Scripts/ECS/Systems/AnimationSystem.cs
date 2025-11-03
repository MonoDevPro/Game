using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.ECS.Components;
using GodotClient.Simulation;

namespace GodotClient.ECS.Systems;

public sealed partial class AnimationSystem(World world)
    : GameSystem(world)
{
    [Query]
    [All<PlayerControlled>]
    private void UpdateAnimations(in Entity e, ref VisualReference visual, in Velocity velocity, in Facing facing)
    {
        if (visual.VisualNode is not PlayerVisual player)
            return;
        
        player.UpdateFacing(new Vector2I(facing.DirectionX, facing.DirectionY), velocity.DirectionX != 0 || velocity.DirectionY != 0);
    }
}