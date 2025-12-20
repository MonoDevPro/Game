using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Navigation.Client.Components;
using Game.ECS.Navigation.Shared.Components;

namespace Game.ECS.Navigation.Client.Systems;

/// <summary>
/// Sistema que atualiza estados de animação baseado no movimento.
/// </summary>
public sealed partial class ClientAnimationSystem(World world, float walkThreshold = 0.01f) : BaseSystem<World, float>(world)
{
    private readonly float _walkThreshold = walkThreshold;

    [Query]
    [All<SpriteAnimation, VisualInterpolation>]
    private void UpdateAnimationStates(
        [Data] in float deltaTime,
        ref SpriteAnimation animState,
        ref VisualInterpolation movement)
    {
            // Atualiza tempo da animação
            animState.Time += deltaTime;

            // Determina animação baseado no estado de movimento
            if (movement.IsActive)
            {
                // Está se movendo
                animState.SetClip(AnimationClip.Walk);
                animState.Facing = movement.Direction;
            }
            else
            {
                // Parado
                animState.SetClip(AnimationClip.Idle);
            }

            // Atualiza frame index (exemplo:  4 frames por segundo)
            int framesPerSecond = animState.Clip switch
            {
                AnimationClip.Walk => 8,
                AnimationClip.Run => 12,
                _ => 4
            };

            animState.Frame = (int)(animState.Time * framesPerSecond) % GetFrameCount(animState.Clip);
    }

    private static int GetFrameCount(AnimationClip type)
    {
        return type switch
        {
            AnimationClip.Idle => 2,
            AnimationClip.Walk => 4,
            AnimationClip.Run => 6,
            _ => 1
        };
    }
}