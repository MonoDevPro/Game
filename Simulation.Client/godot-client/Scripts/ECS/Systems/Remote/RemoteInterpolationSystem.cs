using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.ECS.Components;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Sistema de interpolação VISUAL para jogadores remotos.
/// NÃO modifica componentes lógicos (Position, Velocity, Facing).
/// Apenas atualiza a posição visual do node.
/// </summary>
public sealed partial class RemoteInterpolationSystem(World world)
    : GameSystem(world)
{
    private const float PixelsPerCell = 32f;
    private const float LerpSpeed = 0.15f;

    [Query]
    [All<RemotePlayerTag, Position, Velocity, VisualReference>]
    private void InterpolatePosition(
        in Entity e, 
        in Position pos, 
        in Velocity velocity,
        ref VisualReference node, 
        [Data] float dt)
    {
        if (!node.IsVisible) 
            return;

        Vector2 current = node.VisualNode.GlobalPosition;
        Vector2 target = new(pos.X * PixelsPerCell, pos.Y * PixelsPerCell);

        // Extrapolação para compensar latência
        if (velocity is not { DirectionX: 0, DirectionY: 0, Speed: > 0f })
        {
            float extrapolation = 0.5f;
            Vector2 direction = new(velocity.DirectionX, velocity.DirectionY);
            target += direction * (PixelsPerCell * extrapolation);
        }

        // Interpolação suave
        Vector2 next = current.Lerp(target, LerpSpeed);
        node.VisualNode.GlobalPosition = next;

        // Snap quando muito próximo
        const float snapThreshold = 2f;
        if (current.DistanceSquaredTo(target) <= snapThreshold * snapThreshold)
        {
            node.VisualNode.GlobalPosition = target;
        }
    }
    
    // ❌ REMOVIDO: UpdateFacing e UpdateAnimations
    // Esses agora estão em PlayerAnimationSystem (compartilhado)
}