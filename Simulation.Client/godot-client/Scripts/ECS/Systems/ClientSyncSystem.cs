using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.Extensions;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Network.Adapters;
using LiteNetLib;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Sistema responsável por coletar componentes marcados como dirty e emitir atualizações de estado.
/// </summary>
public sealed partial class ClientSyncSystem(World world, PacketSender sender) : GameSystem(world)
{
    [Query]
    [All<LocalPlayerTag, NetworkId, DirtyFlags>]
    private void SyncToServer(
        in Entity entity,
        ref DirtyFlags dirty)
    {
        if (dirty.IsEmpty) return;
            
        var dirtyFlags = dirty.ConsumeSnapshot();

        // Capture snapshot of dirty flags
        if (dirtyFlags.HasFlag(DirtyComponentType.Input) && World.TryGet(entity, out PlayerInput input))
        {
            var inputPacket = input.ToPlayerInputPacket();
            sender.SendToServer(ref inputPacket, NetworkChannel.Simulation, DeliveryMethod.ReliableOrdered);
        }
    }
}