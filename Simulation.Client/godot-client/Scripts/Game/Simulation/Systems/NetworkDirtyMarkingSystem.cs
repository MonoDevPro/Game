using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.ECS.Utils;

namespace GodotClient.Game.Simulation.Systems;

public sealed partial class NetworkDirtyMarkingSystem(World world) : GameSystem(world)
{
    [Query]
    [All<PlayerControlled, PlayerInput, NetworkDirty>]
    private void MarkMovementDirty(in Entity e, in PlayerInput input, ref NetworkDirty dirty)
    {
        if (input.InputX != 0 || input.InputY != 0 || input.Flags != 0)
        {
            dirty.AddFlags(SyncFlags.Movement);
        }
    }

    [Query]
    [All<PlayerControlled, Facing, Velocity, NetworkDirty>]
    private void MarkFacingDirty(in Entity e, in Facing facing, in Velocity vel, ref NetworkDirty dirty)
    {
        if ((facing.DirectionX != 0 || facing.DirectionY != 0) &&
            (vel.DirectionX != 0 || vel.DirectionY != 0))
        {
            dirty.AddFlags(SyncFlags.Facing);
        }
    }
}