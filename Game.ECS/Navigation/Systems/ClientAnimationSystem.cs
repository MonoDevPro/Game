using Arch.Core;
using Arch.System;
using Game.ECS.Navigation.Components;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Sistema que atualiza estados de animação baseado no movimento.
/// </summary>
public sealed class ClientAnimationSystem : BaseSystem<World, float>
{
    private readonly float _walkThreshold;

    public ClientAnimationSystem(World world, float walkThreshold = 0.01f) : base(world)
    {
        _walkThreshold = walkThreshold;
    }

    public override void Update(in float deltaTime)
    {
        UpdateAnimationStates(World, deltaTime);
    }

    private void UpdateAnimationStates(World world, float deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<AnimationState, ClientMovementState, ClientNavigationEntity>();

        world.Query(in query, (
            ref AnimationState animState,
            ref ClientMovementState movement) =>
        {
            // Atualiza tempo da animação
            animState.AnimationTime += deltaTime;

            // Determina animação baseado no estado de movimento
            if (movement.IsInterpolating)
            {
                // Está se movendo
                animState.SetAnimation(AnimationType.Walking);
                animState.FacingDirection = movement.Direction;
            }
            else
            {
                // Parado
                animState.SetAnimation(AnimationType. Idle);
            }

            // Atualiza frame index (exemplo:  4 frames por segundo)
            int framesPerSecond = animState.CurrentAnimation switch
            {
                AnimationType. Walking => 8,
                AnimationType.Running => 12,
                _ => 4
            };

            animState. FrameIndex = (int)(animState.AnimationTime * framesPerSecond) % GetFrameCount(animState.CurrentAnimation);
        });
    }

    private static int GetFrameCount(AnimationType type)
    {
        return type switch
        {
            AnimationType. Idle => 2,
            AnimationType.Walking => 4,
            AnimationType.Running => 6,
            _ => 1
        };
    }
}

/// <summary>
/// Sistema que atualiza facing direction com rotação suave (opcional).
/// </summary>
public sealed class FacingInterpolationSystem : BaseSystem<World, float>
{
    public FacingInterpolationSystem(World world) : base(world) { }

    public override void Update(in float deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<AnimationState, ClientMovementState, ClientVisualSettings, ClientNavigationEntity>();

        World.Query(in query, (
            ref AnimationState animState,
            ref ClientMovementState movement,
            ref ClientVisualSettings settings) =>
        {
            if (!settings.SmoothRotation)
            {
                // Atualização instantânea
                if (movement.Direction != MovementDirection.None)
                {
                    animState.FacingDirection = movement.Direction;
                }
                return;
            }

            // Rotação suave poderia ser implementada aqui
            // Para jogos 2D top-down, geralmente não é necessário
            if (movement.Direction != MovementDirection.None)
            {
                animState.FacingDirection = movement.Direction;
            }
        });
    }
}