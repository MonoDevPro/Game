using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Modules.Navigation.Client.Components;

namespace GameECS.Modules.Navigation.Client.Systems;

/// <summary>
/// Sistema que interpola posições visuais para movimento suave.
/// </summary>
public sealed partial class ClientInterpolationSystem : BaseSystem<World, float>
{
    private readonly float _cellSize;
    private float _currentDeltaTime;

    public ClientInterpolationSystem(World world, float cellSize = 32f) : base(world)
    {
        _cellSize = cellSize;
    }

    public override void Update(in float deltaTime)
    {
        _currentDeltaTime = deltaTime;
        ProcessInterpolationQuery(World);
        ProcessBufferedMovementsQuery(World);
    }

    [Query]
    [All<VisualPosition, VisualInterpolation, ClientVisualConfig, ClientNavigationEntity>]
    private void ProcessInterpolation(
        ref VisualPosition visualPos,
        ref VisualInterpolation movement,
        ref ClientVisualConfig settings)
    {
        if (!movement.IsActive)
            return;

        if (!settings.SmoothMovement)
        {
            // Sem interpolação - snap direto
            visualPos = movement.To;
            movement.Finish();
            return;
        }

        // Atualiza progresso
        if (movement.Duration > 0)
        {
            movement.Progress += _currentDeltaTime / movement.Duration;
        }
        else
        {
            movement.Progress = 1f;
        }

        // Clamp
        if (movement.Progress >= 1f)
        {
            movement.Progress = 1f;
            visualPos = movement.To;
            movement.Finish();
            return;
        }

        // Aplica easing
        float easedProgress = ApplyEasing(movement.Progress, settings.Easing);

        // Interpola posição
        visualPos = VisualPosition.Lerp(movement.From, movement.To, easedProgress);
    }

    [Query]
    [All<VisualPosition, VisualInterpolation, MovementQueue, ClientVisualConfig, ClientNavigationEntity>]
    private void ProcessBufferedMovements(
        ref VisualPosition visualPos,
        ref VisualInterpolation movement,
        ref MovementQueue buffer,
        ref ClientVisualConfig settings)
    {
        // Se terminou interpolação atual e tem mais no buffer
        if (!movement.IsActive && buffer.HasItems)
        {
            if (buffer.TryDequeue(out int targetX, out int targetY, out float duration, out var direction))
            {
                var targetPos = VisualPosition.FromGrid(targetX, targetY, _cellSize);
                movement.Start(visualPos, targetPos, duration, direction);
            }
        }
    }

    private static float ApplyEasing(float t, EasingType type)
    {
        return type switch
        {
            EasingType.Linear => t,
            EasingType.QuadIn => t * t,
            EasingType.QuadOut => 1f - (1f - t) * (1f - t),
            EasingType.QuadInOut => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
            EasingType.SmoothStep => t * t * (3f - 2f * t),
            EasingType.SmootherStep => t * t * t * (t * (6f * t - 15f) + 10f),
            _ => t
        };
    }
}