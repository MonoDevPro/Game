using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por coletar componentes marcados como dirty e emitir atualizações de estado.
/// </summary>
public sealed partial class ServerSyncSystem(World world, INetworkManager sender) : GameSystem(world)
{
    [Query]
    [All<LocalPlayerTag, NetworkId, DirtyFlags>]
    private void SyncDirtyEntities(
        in Entity entity,
        in NetworkId networkId,
        ref DirtyFlags dirty)
    {
        if (dirty.IsEmpty) return;

        // Capture snapshot of dirty flags
        var dirtyFlags = dirty.ConsumeSnapshot();

        if (dirtyFlags.HasFlag(DirtyComponentType.Position | DirtyComponentType.Facing) &&
            World.TryGet(entity, out Position position) && World.TryGet(entity, out Facing facing))
        {
            var updatePacket = new PlayerStatePacket(networkId.Value, position, facing);
            sender.SendToAll(updatePacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }

        if (dirtyFlags.HasFlag(DirtyComponentType.Health | DirtyComponentType.Mana) &&
            World.TryGet(entity, out Health health) && World.TryGet(entity, out Mana mana))
        {
            var updatePacket = new PlayerVitalsPacket(networkId.Value, health, mana);
            sender.SendToAll(updatePacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
    }
}