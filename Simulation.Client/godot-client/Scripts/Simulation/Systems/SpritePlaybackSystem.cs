using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace GodotClient.Simulation.Systems;

// Aplica o AnimationState no AnimatedSprite2D
public sealed partial class SpritePlaybackSystem(World world) : GameSystem(world)
{
    [Query]
    [All<SpriteRef, AnimationState, VisualAnimSet>]
    private void Play(in Entity e, ref SpriteRef spriteRef, in AnimationState anim, in VisualAnimSet set)
    {
        var sprite = spriteRef.Sprite2D;

        string target = anim.State switch
        {
            AnimState.Idle   => set.Idle ?? "idle",
            AnimState.Walk   => set.Walk ?? "walk",
            AnimState.Run    => set.Run  ?? set.Walk ?? "walk",
            AnimState.Attack => set.Attack ?? "attack",
            AnimState.Hurt   => set.Hurt ?? "hurt",
            AnimState.Dead   => set.Dead ?? "dead",
            _ => set.Idle ?? "idle"
        };

        if (sprite.Animation != target)
        {
            sprite.Animation = target;
            sprite.Play();
        }

        sprite.SpeedScale = anim.Speed <= 0f ? 1f : anim.Speed;
        sprite.SpriteFrames.SetAnimationLoop(target, anim.Loop);

        // Flip por Facing
        if (World.TryGet(e, out Facing facing) && facing.DirectionX != 0)
        {
            sprite.FlipH = facing.DirectionX < 0;
        }
    }
}