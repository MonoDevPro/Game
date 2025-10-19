using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace GodotClient.Simulation.Systems;

// Atualiza ZIndex baseado no Y (top-down) ou mantém BaseZ
public sealed partial class YSortSystem(World world) : GameSystem(world)
{
    [Query]
    [All<Position, NodeRef, Sorting>]
    private void ApplyYSort(in Entity e, in Position pos, ref NodeRef node, in Sorting sorting)
    {
        if (!node.IsVisible) return;

        if (sorting.UseYSort)
            node.Node2D.ZIndex = sorting.BaseZ + pos.Y * sorting.YSortMultiplier;
        else
            node.Node2D.ZIndex = sorting.BaseZ;
    }
}