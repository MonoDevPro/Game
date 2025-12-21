using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Modules.Navigation.Client.Components;
using GameECS.Modules.Navigation.Shared.Components;

namespace GameECS.Modules.Navigation.Client.Systems;

/// <summary>
/// Sistema que atualiza estados de animação baseado no movimento.
/// </summary>
public sealed partial class ClientAnimationSystem : BaseSystem<World, float>
{
    private readonly float _walkThreshold;
    private float _currentDeltaTime;

    public ClientAnimationSystem(World world, float walkThreshold = 0.01f) : base(world)
    {
        _walkThreshold = walkThreshold;
    }

    public override void Update(in float deltaTime)
    {
        _currentDeltaTime = deltaTime;
        UpdateAnimationStatesQuery(World);
    }

    [Query]
    [All<SpriteAnimation, VisualInterpolation>]
    private void UpdateAnimationStates(ref SpriteAnimation animState, ref VisualInterpolation movement)
    {
        // Atualiza tempo da animação
        animState.Time += _currentDeltaTime;

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

        // Atualiza frame index (exemplo: 4 frames por segundo)
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

/// <summary>
/// Sistema que atualiza facing direction com rotação suave (opcional).
/// </summary>
public sealed partial class FacingInterpolationSystem : BaseSystem<World, float>
{
    public FacingInterpolationSystem(World world) : base(world) { }

    public override void Update(in float deltaTime)
    {
        UpdateFacingQuery(World);
    }

    [Query]
    [All<SpriteAnimation, VisualInterpolation, ClientVisualConfig, ClientNavigationEntity>]
    private void UpdateFacing(
        ref SpriteAnimation animState,
        ref VisualInterpolation movement,
        ref ClientVisualConfig settings)
    {
        if (!settings.SmoothMovement)
        {
            // Atualização instantânea
            if (movement.Direction != MovementDirection.None)
            {
                animState.Facing = movement.Direction;
            }
            return;
        }

        // Rotação suave poderia ser implementada aqui
        // Para jogos 2D top-down, geralmente não é necessário
        if (movement.Direction != MovementDirection.None)
        {
            animState.Facing = movement.Direction;
        }
    }
}