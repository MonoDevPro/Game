using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.ECS.Utils;
using Game.Network.Abstractions;

namespace GodotClient.ECS.Systems;

/// <summary>
/// Sistema de sincronização de rede de alta performance.
/// Coleta e envia atualizações de estado dos jogadores usando queries diretas do Arch ECS.
/// Integrado com sistema de predição para rastrear inputs pendentes.
/// 
/// Autor: MonoDevPro
/// Data: 2025-01-11 01:39:21
/// </summary>
public sealed partial class NetworkSenderSystem(World world, INetworkManager networkManager)
{
    public void SendInputToServer(Entity entity, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if 
        
        networkManager.SendToServer(input, NetworkChannel.Simulation, 
            NetworkDeliveryMethod.ReliableOrdered);
        
        // Limpa dirty flag
        dirty.RemoveFlags(SyncFlags.Movement);
    }

    /// <summary>
    /// Envia atualizações de facing/animação para sincronização remota
    /// </summary>
    [Query]
    [All<PlayerControlled, Facing, NetworkDirty>]
    private void SendFacingToServer(in Entity entity, in Facing facing, ref NetworkDirty dirty)
    {
        if (!dirty.HasFlags(SyncFlags.Facing))
            return;
        
        networkManager.SendToServer(facing, NetworkChannel.Simulation, 
            NetworkDeliveryMethod.ReliableOrdered);
    
        dirty.RemoveFlags(SyncFlags.Facing);
    }
}