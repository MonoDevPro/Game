using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Godot;

namespace GodotClient.Game.Simulation.Systems;

// Suaviza render dos remotos. O Position continua autoritativo.
// Apenas o Node2D é interpolado; quando chega próximo do alvo, commit no Position opcionalmente.
public sealed partial class RemoteInterpolationSystem(World world) : GameSystem(world)
{
    private const float PixelsPerCell = 32f;

    [Query]
    [All<Position, NodeRef, RemoteInterpolation>]
    private void LerpRemote(in Entity e, ref Position pos, ref NodeRef node, in RemoteInterpolation interp, [Data] float dt)
    {
        if (!node.IsVisible) return;

        Vector2 current = node.Node2D.GlobalPosition;
        Vector2 target  = new(pos.X * PixelsPerCell, pos.Y * PixelsPerCell);

        Vector2 next = current.Lerp(target, interp.LerpAlpha <= 0f ? 0.15f : interp.LerpAlpha);
        node.Node2D.GlobalPosition = next;

        if ((next - target).LengthSquared() <= (interp.ThresholdPx <= 0f ? 1f : interp.ThresholdPx) * (interp.ThresholdPx <= 0f ? 1f : interp.ThresholdPx))
        {
            // opcional: commit pos visual ao alvo
            node.Node2D.GlobalPosition = target;
        }
    }
}