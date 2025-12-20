using Arch.Core;
using Arch.System;
using Game.ECS.Navigation.Components;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Sistema que interpola posições visuais para movimento suave.
/// </summary>
public sealed class ClientMovementInterpolationSystem : BaseSystem<World, float>
{
    private readonly float _cellSize;

    public ClientMovementInterpolationSystem(World world, float cellSize = 32f) : base(world)
    {
        _cellSize = cellSize;
    }

    public override void Update(in float deltaTime)
    {
        ProcessInterpolation(World, deltaTime);
        ProcessBufferedMovements(World);
    }

    private void ProcessInterpolation(World world, float deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<VisualPosition, ClientMovementState, ClientVisualSettings, ClientNavigationEntity>();

        world.Query(in query, (
            ref VisualPosition visualPos,
            ref ClientMovementState movement,
            ref ClientVisualSettings settings) =>
        {
            if (! movement.IsInterpolating)
                return;

            if (! settings.SmoothMovement)
            {
                // Sem interpolação - snap direto
                visualPos = movement.ToPosition;
                movement.Complete();
                return;
            }

            // Atualiza progresso
            if (movement.Duration > 0)
            {
                movement.Progress += deltaTime / movement.Duration;
            }
            else
            {
                movement.Progress = 1f;
            }

            // Clamp
            if (movement.Progress >= 1f)
            {
                movement.Progress = 1f;
                visualPos = movement.ToPosition;
                movement.Complete();
                return;
            }

            // Aplica easing
            float easedProgress = ApplyEasing(movement.Progress, settings.EasingFunction);

            // Interpola posição
            visualPos = VisualPosition.Lerp(movement.FromPosition, movement.ToPosition, easedProgress);
        });
    }

    private void ProcessBufferedMovements(World world)
    {
        var query = new QueryDescription()
            .WithAll<VisualPosition, ClientMovementState, MovementBuffer, ClientVisualSettings, ClientNavigationEntity>();

        world.Query(in query, (
            ref VisualPosition visualPos,
            ref ClientMovementState movement,
            ref MovementBuffer buffer,
            ref ClientVisualSettings settings) =>
        {
            // Se terminou interpolação atual e tem mais no buffer
            if (! movement.IsInterpolating && buffer.HasPending)
            {
                if (buffer.TryDequeue(out int targetX, out int targetY, out float duration, out var direction))
                {
                    var targetPos = VisualPosition.FromGrid(targetX, targetY, _cellSize);
                    movement.StartInterpolation(visualPos, targetPos, duration, direction);
                }
            }
        });
    }

    private static float ApplyEasing(float t, EasingType type)
    {
        return type switch
        {
            EasingType.Linear => t,
            EasingType.QuadIn => t * t,
            EasingType. QuadOut => 1f - (1f - t) * (1f - t),
            EasingType.QuadInOut => t < 0.5f ? 2f * t * t : 1f - MathF.Pow(-2f * t + 2f, 2f) / 2f,
            EasingType.SmoothStep => t * t * (3f - 2f * t),
            EasingType.SmootherStep => t * t * t * (t * (6f * t - 15f) + 10f),
            _ => t
        };
    }
}