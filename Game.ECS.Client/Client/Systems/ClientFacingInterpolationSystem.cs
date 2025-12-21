using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Client.Client.Components;
using Game.ECS.Shared.Components.Navigation;

namespace Game.ECS.Client.Client.Systems;

/// <summary>
/// Sistema que atualiza facing direction com rotação suave (opcional).
/// </summary>
public sealed partial class ClientFacingInterpolationSystem(World world) : BaseSystem<World, float>(world)
{
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