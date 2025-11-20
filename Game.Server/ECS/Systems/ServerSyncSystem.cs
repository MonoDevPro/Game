using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.Extensions;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;

namespace Game.Server.ECS.Systems;

public sealed partial class ServerSyncSystem(
    World world,
    INetworkManager sender,
    ILogger<ServerSyncSystem>? logger = null)
    : GameSystem(world)
{
    private readonly List<NpcSpawnSnapshot> _npcSpawnBuffer = [];
    private readonly List<NpcStateSnapshot> _npcStateBuffer = [];
    
    private readonly List<PlayerSnapshot> _playerSpawnBuffer = [];
    private readonly List<PlayerStateSnapshot> _playerStateBuffer = [];
    private readonly List<PlayerVitalsSnapshot> _playerVitalsBuffer = [];
    private readonly List<CombatStateSnapshot> _playerCombatBuffer = [];

    [Query]
    [All<PlayerControlled, NetworkId, DirtyFlags>]
    private void SyncPlayers(
        in Entity entity,
        in NetworkId networkId,
        ref DirtyFlags dirty)
    {
        if (dirty.IsEmpty) return;
        
        // Estado completo para spawns
        if (dirty.IsDirtyMask(DirtyComponentType.All))
        {
            _playerSpawnBuffer.Add(World.BuildPlayerDataSnapshot(entity).ToPlayerSpawnSnapshot());
            dirty.ClearDirtyMask(DirtyComponentType.All);
        }

        // Estado de movimento
        if (dirty.IsDirty(DirtyComponentType.State))
            _playerStateBuffer.Add(World.BuildPlayerStateSnapshot(entity).ToPlayerStateSnapshot());

        // Vitals
        if (dirty.IsDirty(DirtyComponentType.Vitals))
            _playerVitalsBuffer.Add(World.BuildPlayerVitalsSnapshot(entity).ToPlayerVitalsSnapshot());
        
        // Combate (estado + resultado)
        if (dirty.IsDirty(DirtyComponentType.Combat) &&
            World.TryGet(entity, out CombatState combat) &&
            World.TryGet(entity, out Attack attackAction))
        {
            _playerCombatBuffer.Add(new CombatStateSnapshot(
                AttackerNetworkId: networkId.Value,
                Type: attackAction.Type,
                AttackDuration: attackAction.TotalDuration,
                CooldownRemaining: combat.LastAttackTime
            ));
        }
        
        dirty.ClearAll();
    }
    
    [Query]
    [All<AIControlled, NetworkId, DirtyFlags>]
    private void SyncNpcs(
        in Entity entity,
        ref DirtyFlags dirty)
    {
        if (dirty.IsEmpty) return;
        
        // Estado completo para spawns
        if (dirty.IsDirtyMask(DirtyComponentType.All))
        {
            var npcData = World.BuildNPCSnapshot(entity).ToNpcSpawnData();
            _npcSpawnBuffer.Add(npcData);
        }

        if (dirty.IsDirtyMask(DirtyComponentType.State | DirtyComponentType.Vitals))
        {
            var stateData = World.BuildNpcStateSnapshot(entity).ToNpcStateSnapshot();
            _npcStateBuffer.Add(stateData);
        }
        
        dirty.ClearAll();
    }

    public override void AfterUpdate(in float deltaTime)
    {
        if (_npcSpawnBuffer.Count > 0)
        {
            var packet = new NpcSpawnPacket(_npcSpawnBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _npcSpawnBuffer.Clear();
        }

        if (_npcStateBuffer.Count > 0)
        {
            var packet = new NpcStatePacket(_npcStateBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _npcStateBuffer.Clear();
        }
        
        if (_playerSpawnBuffer.Count > 0)
        {
            var packet = new PlayerSpawnPacket(_playerSpawnBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _playerSpawnBuffer.Clear();
        }
        
        if (_playerStateBuffer.Count > 0)
        {
            var packet = new PlayerStatePacket(_playerStateBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _playerStateBuffer.Clear();
        }
        
        if (_playerVitalsBuffer.Count > 0)
        {
            var packet = new PlayerVitalsPacket(_playerVitalsBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _playerVitalsBuffer.Clear();
        }
        
        if (_playerCombatBuffer.Count > 0)
        {
            var packet = new CombatStatePacket(_playerCombatBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _playerCombatBuffer.Clear();
        }
        
    }
}