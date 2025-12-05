using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Schema.Components;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;

namespace Game.Server.ECS.Systems;

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
    private readonly List<StateData> _stateUpdates = new(32);
    private readonly List<VitalsData> _vitalsUpdates = new(32);
    
    // Sync interval tracking
    private float _stateAccumulator;
    private float _vitalsAccumulator;
    private const float StateUpdateInterval = 0.05f;  // 20Hz for position updates
    private const float VitalsUpdateInterval = 0.5f;   // 2Hz for vitals updates

    public override void BeforeUpdate(in float deltaTime)
    {
        _stateAccumulator += deltaTime;
        _vitalsAccumulator += deltaTime;
    }

    public override void Update(in float deltaTime)
    {
        // Collect state updates (position/movement)
        if (_stateAccumulator >= StateUpdateInterval)
        {
            CollectStateUpdatesQuery(World);
            
            SendStateUpdates();
            _stateAccumulator = 0f;
        }
        
        // Collect vitals updates (HP/MP)
        if (_vitalsAccumulator >= VitalsUpdateInterval)
        {
            CollectVitalsUpdatesQuery(World);
            
            SendVitalsUpdates();
            _vitalsAccumulator = 0f;
        }
    }

    #region Player State Collection
    
    [Query]
    [All<NetworkId, Position, Floor, Speed, Direction, Walkable>]
    [Any<PlayerControlled, AIControlled>]
    private void CollectStateUpdates(
        in NetworkId networkId,
        in Position position,
        in Floor floor,
        in Speed speed,
        in Direction direction)
    {
        _stateUpdates.Add(new StateData(
            NetworkId: networkId.Value,
            X: position.X,
            Y: position.Y,
            Floor: floor.Value,
            Speed: speed.Value,
            DirX: direction.X,
            DirY: direction.Y
        ));
    }
    
    [Query]
    [All<PlayerControlled, NetworkId, Health, Mana>]
    private void CollectVitalsUpdates(
        in NetworkId networkId,
        in Health health,
        in Mana mana)
    {
        _vitalsUpdates.Add(new VitalsData(
            NetworkId: networkId.Value,
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max
        ));
    }
    
    #endregion
    
    #region Send Updates
    
    private void SendStateUpdates()
    {
        // Send player state updates
        if (_stateUpdates.Count > 0)
        {
            var packet = new StatePacket([.._stateUpdates]);
            networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.Unreliable);
            _stateUpdates.Clear();
        }
    }
    
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
