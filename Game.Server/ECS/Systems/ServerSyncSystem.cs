using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.Extensions;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Schema.Components;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;
using Microsoft.Extensions.Logging;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por sincronizar o estado das entidades com os clientes conectados.
/// Envia atualizações de posição e vitals para todos os peers.
/// </summary>
public sealed partial class ServerSyncSystem : GameSystem
{
    private readonly INetworkManager _networkManager;
    private readonly ILogger<ServerSyncSystem>? _logger;
    
    // Buffers for batching updates
    private readonly List<StateUpdate> _playerStateUpdates = new(32);
    private readonly List<VitalsUpdate> _playerVitalsUpdates = new(32);
    private readonly List<NpcStateUpdate> _npcStateUpdates = new(64);
    private readonly List<NpcVitalsUpdate> _npcVitalsUpdates = new(64);
    
    // Sync interval tracking
    private float _stateAccumulator;
    private float _vitalsAccumulator;
    private const float StateUpdateInterval = 0.05f;  // 20Hz for position updates
    private const float VitalsUpdateInterval = 0.5f;   // 2Hz for vitals updates

    public ServerSyncSystem(World world, INetworkManager networkManager, ILogger<ServerSyncSystem>? logger = null) 
        : base(world, logger)
    {
        _networkManager = networkManager;
        _logger = logger;
    }

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
            CollectPlayerStateUpdatesQuery(World);
            CollectNpcStateUpdatesQuery(World);
            
            SendStateUpdates();
            _stateAccumulator = 0f;
        }
        
        // Collect vitals updates (HP/MP)
        if (_vitalsAccumulator >= VitalsUpdateInterval)
        {
            CollectPlayerVitalsUpdatesQuery(World);
            CollectNpcVitalsUpdatesQuery(World);
            
            SendVitalsUpdates();
            _vitalsAccumulator = 0f;
        }
    }

    #region Player State Collection
    
    [Query]
    [All<PlayerControlled, NetworkId, Position, Floor, Speed, Direction, Walkable>]
    [None<Dead>]
    private void CollectPlayerStateUpdates(
        in NetworkId networkId,
        in Position position,
        in Floor floor,
        in Speed speed,
        in Direction direction)
    {
        _playerStateUpdates.Add(new StateUpdate(
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
    private void CollectPlayerVitalsUpdates(
        in NetworkId networkId,
        in Health health,
        in Mana mana)
    {
        _playerVitalsUpdates.Add(new VitalsUpdate(
            NetworkId: networkId.Value,
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max
        ));
    }
    
    #endregion
    
    #region NPC State Collection
    
    [Query]
    [All<AIControlled, NetworkId, Position, Floor, Speed, Direction>]
    [None<Dead>]
    private void CollectNpcStateUpdates(
        in NetworkId networkId,
        in Position position,
        in Floor floor,
        in Speed speed,
        in Direction direction)
    {
        _npcStateUpdates.Add(new NpcStateUpdate(
            NetworkId: networkId.Value,
            X: position.X,
            Y: position.Y,
            Speed: speed.Value,
            DirectionX: direction.X,
            DirectionY: direction.Y
        ));
    }
    
    [Query]
    [All<AIControlled, NetworkId, Health, Mana>]
    private void CollectNpcVitalsUpdates(
        in NetworkId networkId,
        in Health health,
        in Mana mana)
    {
        _npcVitalsUpdates.Add(new NpcVitalsUpdate(
            NetworkId: networkId.Value,
            CurrentHp: health.Current,
            CurrentMp: mana.Current
        ));
    }
    
    #endregion
    
    #region Send Updates
    
    private void SendStateUpdates()
    {
        // Send player state updates
        if (_playerStateUpdates.Count > 0)
        {
            var packet = new PlayerStatePacket([.._playerStateUpdates]);
            _networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.Unreliable);
            _playerStateUpdates.Clear();
        }
        
        // Send NPC state updates
        if (_npcStateUpdates.Count > 0)
        {
            var packet = new NpcStatePacket([.._npcStateUpdates]);
            _networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.Unreliable);
            _npcStateUpdates.Clear();
        }
    }
    
    private void SendVitalsUpdates()
    {
        // Send player vitals updates
        if (_playerVitalsUpdates.Count > 0)
        {
            var packet = new PlayerVitalsPacket([.._playerVitalsUpdates]);
            _networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _playerVitalsUpdates.Clear();
        }
        
        // Send NPC vitals updates  
        if (_npcVitalsUpdates.Count > 0)
        {
            var packet = new NpcHealthPacket([.._npcVitalsUpdates]);
            _networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _npcVitalsUpdates.Clear();
        }
    }
    
    #endregion
}
