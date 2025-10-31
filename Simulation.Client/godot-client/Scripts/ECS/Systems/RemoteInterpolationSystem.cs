using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;
using GodotClient.ECS.Components;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Sistema de interpolação para jogadores remotos.
/// Suaviza a renderização entre snapshots do servidor.
/// </summary>
public sealed partial class RemoteInterpolationSystem(World world)
    : GameSystem(world)
{
    private const float PixelsPerCell = 32f;

    [Query]
    [All<Position, VisualReference, RemoteInterpolation, RemotePlayerTag>]
    private void LerpRemote(in Entity e, ref Position pos, ref VisualReference node, in RemoteInterpolation interp, [Data] float dt)
    {
        if (!node.IsVisible) 
            return;

        Vector2 current = node.VisualNode.GlobalPosition;
        Vector2 target  = new(pos.X * PixelsPerCell, pos.Y * PixelsPerCell);

        float alpha = interp.LerpAlpha <= 0f ? 0.15f : interp.LerpAlpha;
        Vector2 next = current.Lerp(target, alpha);
        node.VisualNode.GlobalPosition = next;

        float threshold = interp.ThresholdPx <= 0f ? 2f : interp.ThresholdPx;
        if ((next - target).LengthSquared() <= threshold * threshold)
        {
            // Snap ao alvo quando próximo o suficiente
            node.VisualNode.GlobalPosition = target;
        }
    }
}