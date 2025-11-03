using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.ECS.Components;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Atualiza o nó visual do jogador local.
/// Move suavemente dentro da célula usando o Movement.Timer como fração do avanço entre tiles.
/// </summary>
public sealed partial class LocalVisualUpdateSystem(World world)
    : GameSystem(world)
{
    private const float PixelsPerCell = 32f;

    [Query]
    [All<LocalPlayerTag, Position, Movement, Velocity, VisualReference>]
    private void UpdateLocalNode(in Entity e, in Position pos, in Movement movement, in Velocity velocity, ref VisualReference node)
    {
        if (!node.IsVisible || node is { VisualNode: null })
            return;

        // Posição base do tile atual, em pixels
        Vector2 basePixels = new(pos.X * PixelsPerCell, pos.Y * PixelsPerCell);

        // Se não há direção, fixa no tile atual
        if (velocity.DirectionX == 0 && velocity.DirectionY == 0)
        {
            node.VisualNode.GlobalPosition = basePixels;
            return;
        }

        // Fração do avanço atual dentro da célula [0..1]
        float fraction = Mathf.Clamp(
            SimulationConfig.CellSize <= 0f ? 0f : movement.Timer / SimulationConfig.CellSize,
            0f, 1f);

        // Deslocamento dentro da célula, em pixels
        Vector2 dir = new(velocity.DirectionX, velocity.DirectionY);
        Vector2 offsetPixels = dir * (fraction * PixelsPerCell);

        node.VisualNode.GlobalPosition = basePixels + offsetPixels;
    }
}