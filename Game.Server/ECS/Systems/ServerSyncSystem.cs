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

        // Snapshot e limpa
        var dirtyFlags = dirty;
        dirty.ClearAll();

        // Estado de movimento
        if (dirtyFlags.IsDirty(
                DirtyComponentType.Position | DirtyComponentType.Facing | DirtyComponentType.Velocity) &&
            World.TryGet(entity, out Position position) && 
            World.TryGet(entity, out Facing facing) &&
            World.TryGet(entity, out Velocity velocity))
        {
            var statePacket = new StatePacket(networkId.Value, position, velocity, facing);
            sender.SendToAll(statePacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }

        // Vitals
        if (dirtyFlags.IsDirty(DirtyComponentType.Health | DirtyComponentType.Mana) &&
            World.TryGet(entity, out Health health) && World.TryGet(entity, out Mana mana))
        {
            var vitalsPacket = new VitalsPacket(networkId.Value, health, mana);
            sender.SendToAll(vitalsPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }

        // Combate (estado + resultado)
        if (!dirtyFlags.IsDirty(DirtyComponentType.CombatState) ||
            !World.TryGet(entity, out CombatState combat)) 
            return;
        
        // Se existir AttackAction, enviamos o estado de ataque (animação)
        if (!World.TryGet(entity, out AttackAction attackAction)) 
            return;
            
        // Pacote de início de animação
        var combatPacket = new CombatStatePacket(
            AttackerNetworkId: networkId.Value,
            DefenderNetworkId: attackAction.DefenderNetworkId,
            Type: attackAction.Type,
            AttackDuration: attackAction.RemainingDuration,
            CooldownRemaining: combat.LastAttackTime
        );

        sender.SendToAll(combatPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);

        // Se o ataque efetivamente irá acertar, também enviamos o resultado
        if (attackAction.WillHit)
        {
            // crítico simples (exemplo: AttackType.Critical)
            bool isCritical = attackAction.Type == AttackType.Critical;

            var resultPacket = new AttackResultPacket(
                AttackerNetworkId: networkId.Value,
                DefenderNetworkId: attackAction.DefenderNetworkId,
                Damage: attackAction.Damage,
                WasHit: true,
                IsCritical: isCritical,
                AnimationType: attackAction.Type,
                TimeToLive: 1.0f // tempo para exibir número de dano
            );

            sender.SendToAll(resultPacket, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
        }
                
        // Consumir AttackAction (não reenviar em próximos ticks)
        World.Remove<AttackAction>(entity);
    }
}