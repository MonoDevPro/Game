using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.ECS.Utils;
using Game.Network.Abstractions;
using Game.Network.Packets.Simulation;
using GodotClient.Simulation;

namespace GodotClient.Systems;

/// <summary>
/// Sistema de sincronização de rede de alta performance.
/// Coleta e envia atualizações de estado dos jogadores usando queries diretas do Arch ECS.
/// Integrado com sistema de predição para rastrear inputs pendentes.
/// 
/// Autor: MonoDevPro
/// Data: 2025-01-11 01:39:21
/// </summary>
public sealed partial class NetworkSenderSystem(World world, INetworkManager networkManager) 
    : GameSystem(world: world)
{
    /// <summary>
    /// Envia input para servidor e registra em buffer de predição
    /// </summary>
    [Query]
    [All<LocalPlayer, PlayerControlled, PlayerInput, NetworkDirty>]
    private void SendInputToServer(in Entity entity, ref PlayerInput input, ref NetworkDirty dirty,
        [Data] double timestamp)
    {
        if (!dirty.HasFlags(SyncFlags.Movement))
            return;
        
        // Cria e envia pacote
        var packet = new PlayerInputPacket(
            input.InputX,
            input.InputY,
            (ushort)input.Flags);
        
        networkManager.SendToServer(packet, NetworkChannel.Simulation, 
            NetworkDeliveryMethod.ReliableOrdered);
        
        // Limpa dirty flag
        dirty.RemoveFlags(SyncFlags.Movement);
    }

    /// <summary>
    /// Envia atualizações de facing/animação para sincronização remota
    /// </summary>
    [Query]
    [All<LocalPlayer, Facing, NetworkDirty>]
    private void SendFacingToServer(in Entity entity, in Facing facing, ref NetworkDirty dirty)
    {
        if (!dirty.HasFlags(SyncFlags.Facing))
            return;
    
        // Aqui você pode enviar um packet de facing se necessário
        // Por exemplo: new PlayerFacingPacket(facing.DirectionX, facing.DirectionY)
        
        dirty.RemoveFlags(SyncFlags.Facing);
    }
}