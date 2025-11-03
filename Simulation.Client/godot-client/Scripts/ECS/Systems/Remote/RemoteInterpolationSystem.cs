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
/// Sistema de interpolação para jogadores remotos.
/// Interpola suavemente entre snapshots do servidor SEM simular movimento.
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

        // Se o jogador está se movendo, adiciona pequena extrapolação
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

    [Query]
    [All<RemotePlayerTag, Velocity, Facing>]
    private void UpdateFacing(in Entity e, in Velocity velocity, ref Facing facing)
    {
        // Facing vem do servidor via snapshot
        if (velocity is not { DirectionX: 0, DirectionY: 0 })
        {
            facing.DirectionX = velocity.DirectionX;
            facing.DirectionY = velocity.DirectionY;
        }
    }

    [Query]
    [All<RemotePlayerTag, PlayerControlled, VisualReference>]
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
    
    // ❌ REMOVIDO: DecayVelocity
    // Velocity de remotos vem APENAS do servidor, nunca é modificada localmente
}