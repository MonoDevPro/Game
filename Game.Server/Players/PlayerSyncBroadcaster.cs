using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Systems.Common;
using Game.Network.Abstractions;
using Game.Network.Packets;
using Game.Network.Packets.Simulation;

namespace Game.Server.Players;

/// <summary>
/// Sistema de sincronização de rede de alta performance.
/// Coleta e broadcasta atualizações de estado dos jogadores usando queries diretas do Arch ECS.
/// Autor: MonoDevPro
/// Data: 2025-01-11 01:39:21
/// </summary>
public sealed partial class PlayerSyncBroadcaster(World world, INetworkManager networkManager) 
    : GameSystem(world)
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Update(in float data)
    {
        BroadcastMovementQuery(World);
        BroadcastVitalsQuery(World);
    }

    /// <summary>
    /// Broadcasta atualizações de movimento (posição + direção).
    /// Usa Sequenced delivery (pode descartar pacotes antigos).
    /// </summary>
    [Query]
    [All<NetworkId, Position, Direction, NetworkDirty>]
    private void BroadcastMovement(in Entity entity, ref NetworkId netId, ref Position pos, ref Direction dir, ref NetworkDirty dirty)
    {
        // ⚡ Query direta sem buffer intermediário (FASTEST)
        // Filtra apenas entidades com movimento dirty
        if (!dirty.HasFlags(SyncFlags.Movement))
            return;
            
        // Cria e envia pacote inline (sem alocação de lista)
        var packet = new PlayerMovementPacket(netId.Value, pos.Value, dir.Value);
        networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableSequenced);
            
        // Limpa dirty flag de movimento
        World.ClearNetworkDirty(entity, SyncFlags.Movement);
    }

    /// <summary>
    /// Broadcasta atualizações de vitals (HP/MP).
    /// Usa ReliableOrdered delivery (garante entrega).
    /// </summary>
    [Query]
    [All<NetworkId, Health, Mana, NetworkDirty>]
    private void BroadcastVitals(in Entity entity, ref NetworkId netId, ref Health health, ref Mana mana, ref NetworkDirty dirty)
    {
        // Filtra apenas entidades com vitals dirty
        if (!dirty.HasFlags(SyncFlags.Vitals))
            return;
            
        // Envia pacote de vitals
        var packet = new PlayerVitalsPacket(
            netId.Value,
            health.Current,
            health.Max,
            mana.Current,
            mana.Max);
            
        networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableSequenced);
            
        // Limpa dirty flag de vitals
        World.ClearNetworkDirty(entity, SyncFlags.Vitals);
    }
}