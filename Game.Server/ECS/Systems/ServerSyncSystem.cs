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
    [All<NetworkId, DirtyFlags>]
    private void SyncDirtyEntities(
        in Entity entity,
        in NetworkId networkId,
        ref DirtyFlags dirty)
    {
        if (dirty.IsEmpty) return;

        // Capture snapshot of dirty flags
        var dirtyFlags = dirty;
        dirty.ClearAll();

        if (dirtyFlags.IsDirty(
                DirtyComponentType.Position | DirtyComponentType.Facing | DirtyComponentType.Velocity) &&
            World.TryGet(entity, out Position position) && 
            World.TryGet(entity, out Facing facing) &&
            World.TryGet(entity, out Velocity velocity))
        {
            var updatePacket = new StatePacket(networkId.Value, position, velocity, facing);
            sender.SendToAll(updatePacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }

        if (dirtyFlags.IsDirty(DirtyComponentType.Health | DirtyComponentType.Mana) &&
            World.TryGet(entity, out Health health) && World.TryGet(entity, out Mana mana))
        {
            var updatePacket = new VitalsPacket(networkId.Value, health, mana);
            sender.SendToAll(updatePacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
        
        // ← NOVO: Sincronizar ataques
        if (dirtyFlags.IsDirty(DirtyComponentType.CombatState) &&
            World.TryGet(entity, out CombatState combat) &&
            World.TryGet(entity, out AttackAnimation attackAnim) &&
            attackAnim.IsActive)
        {
            var attackPacket = new AttackPacket(
                networkId.Value,
                attackAnim.DefenderNetworkId,
                attackAnim.Damage,
                attackAnim.WasHit,
                attackAnim.RemainingDuration,
                attackAnim.AnimationType);

            sender.SendToAll(attackPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
        
    }
}