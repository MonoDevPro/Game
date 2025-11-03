using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.ECS.Components;
using GodotClient.Simulation;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Sistema de animação para TODOS os jogadores (local e remotos).
/// </summary>
public sealed partial class PlayerAnimationSystem(World world)
    : GameSystem(world)
{
    [Query]
    [All<PlayerControlled, VisualReference>] // ✅ Sem tag específica - aplica a TODOS
    private void UpdateAnimations(
        in Entity e, 
        ref VisualReference visual, 
        in Velocity velocity, 
        in Facing facing)
    {
        if (visual.VisualNode is not PlayerVisual player)
            return;

        bool isMoving = velocity is { Speed: > 0f };
        player.UpdateFacing(new Vector2I(facing.DirectionX, facing.DirectionY), isMoving);
    }
}