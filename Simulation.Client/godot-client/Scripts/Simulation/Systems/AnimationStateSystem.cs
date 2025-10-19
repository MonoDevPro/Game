using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace GodotClient.Simulation.Systems;

// Decide o AnimationState baseado em Velocity/Facing/CombatState etc.
public sealed partial class AnimationStateSystem(World world) : GameSystem(world)
{
    private const float RunThreshold = 6.5f; // ajuste conforme seu Walkable.BaseSpeed

    [Query]
    [All<Velocity, AnimationState>]
    private void DecideState(in Entity e, in Velocity vel, ref AnimationState anim)
    {
        // Morto tem prioridade (se existir componente/estado)
        // if (World.TryGet(e, out CombatState combat) && combat.IsDead) { anim.State = AnimState.Dead; anim.Loop = true; anim.Speed = 1f; return; }

        if (vel.DirectionX != 0 || vel.DirectionY != 0)
        {
            anim.State = vel.Speed >= RunThreshold ? AnimState.Run : AnimState.Walk;
            anim.Loop = true;
            anim.Speed = 1f;
        }
        else
        {
            anim.State = AnimState.Idle;
            anim.Loop = true;
            anim.Speed = 1f;
        }
    }
}