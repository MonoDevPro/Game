using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.DTOs;
using Game.DTOs.Player;
using Game.ECS.Components;
using Game.ECS.Events;
using Game.ECS.Systems;
using Game.Network.Abstractions;

namespace Game.Server.Simulation.Systems;

/// <summary>
/// Sistema responsável por sincronizar o estado das entidades com os clientes conectados.
/// Envia atualizações de posição e vitals para todos os peers.
/// </summary>
public sealed partial class ServerSyncSystem(
    World world,
    INetworkManager networkManager,
    GameEventBus bus,
    ILogger<ServerSyncSystem>? logger = null)
    : GameSystem(world, logger)
{
    // Buffers for batching updates
    private readonly List<StateSnapshot> _stateUpdates = new(16);
    private readonly List<VitalsSnapshot> _vitalsUpdates = new(16);
    private readonly List<AttackSnapshot> _attackUpdates = new(16);
    
    // Sync interval tracking
    private float _stateAccumulator;
    private float _vitalsAccumulator;
    private float _attackAccumulator;
    private const float StateUpdateInterval = 0.05f;  // 20Hz for position updates
    private const float VitalsUpdateInterval = 0.5f;   // 2Hz for vitals updates
    private const float AttackUpdateInterval = 0.1f;   // 10Hz for attack updates

    public override void Initialize()
    {
        // Subscribe to attack events
        bus.OnAttack += (evt) =>
        {
            _attackUpdates.Add(new AttackSnapshot(
                AttackerNetworkId: World.Get<NetworkId>(evt.Attacker).Value,
                Style: evt.Style,
                AttackDuration: 1.0f, // Placeholder, should be based on attack style
                CooldownRemaining: 0.0f  // Placeholder, should be based on attack state
            ));
        };
    }


    public override void BeforeUpdate(in float deltaTime)
    {
        _stateAccumulator += deltaTime;
        _vitalsAccumulator += deltaTime;
        _attackAccumulator += deltaTime;
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
    [All<NetworkId, Position, Direction>]
    [Any<PlayerControlled, AIControlled>]
    private void CollectStateUpdates(
        in NetworkId networkId,
        in Position position,
        in Direction direction)
    {
        _stateUpdates.Add(new StateSnapshot(
            NetworkId: networkId.Value,
            X: position.X,
            Y: position.Y,
            Z: position.Z,
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
        _vitalsUpdates.Add(new VitalsSnapshot(
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