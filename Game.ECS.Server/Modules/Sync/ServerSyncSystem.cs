using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Server.Events;
using Game.ECS.Shared.Components.Combat;
using Game.ECS.Shared.Components.Entities;
using Game.ECS.Shared.Core.Entities;
using Game.ECS.Shared.Data.Combat;
using Game.ECS.Shared.Services.Network;
using Game.ECS.Shared.Systems;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Server.Modules.Sync;

/// <summary>
/// Sistema responsável por sincronizar o estado das entidades com os clientes conectados.
/// Envia atualizações de posição e vitals para todos os peers.
/// </summary>
public sealed partial class ServerSyncSystem(
    World world,
    INetworkManager networkManager,
    ILogger<ServerSyncSystem>? logger = null)
    : GameSystem(world, logger)
{
    // Buffers for batching updates
    private readonly List<VitalsData> _vitalsUpdates = new(16);
    private readonly List<AttackData> _attackUpdates = new(16);
    
    // Sync interval tracking
    private float _vitalsAccumulator;
    private float _attackAccumulator;
    private const float VitalsUpdateInterval = 0.5f;   // 2Hz for vitals updates
    private const float AttackUpdateInterval = 0.1f;   // 10Hz for attack updates

    public override void BeforeUpdate(in float deltaTime)
    {
        _vitalsAccumulator += deltaTime;
        _attackAccumulator += deltaTime;
    }

    public override void Update(in float deltaTime)
    {
        // Collect vitals updates (HP/MP)
        if (_vitalsAccumulator >= VitalsUpdateInterval)
        {
            CollectVitalsUpdatesQuery(World);
            
            SendVitalsUpdates();
            _vitalsAccumulator = 0f;
        }

        // Send attack updates
        if (_attackAccumulator >= AttackUpdateInterval && _attackUpdates.Count > 0)
        {
            var packet = new AttackPacket([.._attackUpdates]);
            networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _attackUpdates.Clear();
        }
    }

    #region Player State Collection
    
    [Query]
    [All<PlayerControlled, NetworkId, Health, Mana>]
    private void CollectVitalsUpdates(
        in NetworkId networkId,
        in Health health,
        in Mana mana)
    {
        _vitalsUpdates.Add(new VitalsData(
            Id: networkId.Value,
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max
        ));
    }
    
    #endregion
    
    #region Send Updates
    
    private void SendVitalsUpdates()
    {
        // Send player vitals updates
        if (_vitalsUpdates.Count > 0)
        {
            var packet = new VitalsPacket([.._vitalsUpdates]);
            networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _vitalsUpdates.Clear();
        }
    }
    
    #endregion
}