using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.ECS.Utils;
using Game.Network.Abstractions;
using Game.Network.Packets.Simulation;

namespace Game.Server.Simulation.Systems;

/// <summary>
/// Sistema de sincronização de rede de alta performance.
/// Coleta e broadcasta atualizações de estado dos jogadores usando queries diretas do Arch ECS.
/// Autor: MonoDevPro
/// Data: 2025-01-11 01:39:21
/// </summary>
public sealed partial class PlayerSyncBroadcasterSystem(World world, INetworkManager networkManager) 
    : GameSystem(world)
{
    [Query]
    [All<NetworkId, PlayerInput, NetworkDirty>]
    private void BroadcastInput(in Entity entity, in NetworkId networkId, in PlayerInput input, ref NetworkDirty dirty)
    {
        // ⚡ Query direta sem buffer intermediário (FASTEST)
        // Filtra apenas entidades com movimento dirty
        if (!dirty.HasFlags(SyncFlags.Input))
            return;
        
        var packet = new PlayerInputSnapshot(
            networkId.Value,
            input.InputX,
            input.InputY,
            input.Flags);
        
        networkManager.SendToAllExcept(
            excludePeerId: networkId.Value,
            packet,
            NetworkChannel.Simulation, 
            NetworkDeliveryMethod.ReliableOrdered);
        
        // Limpa dirty flag de movimento
        World.ClearNetworkDirty(
            entity, 
            SyncFlags.Input);
    }
    
    /// <summary>
    /// Broadcasta atualizações de movimento (posição + direção + velocidade).
    /// Usa Sequenced delivery (pode descartar pacotes antigos).
    /// </summary>
    [Query]
    [All<NetworkId, Position, Facing, Walkable, NetworkDirty>]
    private void BroadcastState(in Entity entity, ref NetworkId netId, ref Position pos, ref Facing dir, in Walkable speed, ref NetworkDirty dirty)
    {
        // ⚡ Query direta sem buffer intermediário (FASTEST)
        // Filtra apenas entidades com movimento dirty
        if (!dirty.HasFlags(SyncFlags.Movement | SyncFlags.Facing))
            return;
        
        // Calcula velocidade efetiva (BaseSpeed * CurrentModifier)
        var effectiveSpeed = speed.BaseSpeed * speed.CurrentModifier;
        
        // Cria e envia pacote inline (sem alocação de lista)
        var packet = new PlayerStateSnapshot(
            netId.Value, 
            pos.X, 
            pos.Y, 
            pos.Z, 
            dir.DirectionX, 
            dir.DirectionY, 
            effectiveSpeed);
        
        networkManager.SendToAll(
            packet, 
            NetworkChannel.Simulation, 
            NetworkDeliveryMethod.ReliableOrdered);
        
        // Limpa dirty flag de movimento
        World.ClearNetworkDirty(
            entity, 
            SyncFlags.Movement | SyncFlags.Facing);
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
        var packet = new PlayerVitalsSnapshot(
            netId.Value,
            health.Current,
            health.Max,
            mana.Current,
            mana.Max);
        
        networkManager.SendToAll(
            packet, 
            NetworkChannel.Simulation, 
            NetworkDeliveryMethod.ReliableOrdered);
        
        // Limpa dirty flag de vitals
        World.ClearNetworkDirty(
            entity, 
            SyncFlags.Vitals);
    }
}