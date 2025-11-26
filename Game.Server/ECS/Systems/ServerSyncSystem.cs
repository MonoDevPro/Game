using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Core.Extensions;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Logic;
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
    private readonly List<CombatStateSnapshot> _combatBuffer = [];
    
    private readonly List<NpcSpawnSnapshot> _npcSpawnBuffer = [];
    private readonly List<NpcStateSnapshot> _npcStateBuffer = [];
    private readonly List<NpcHealthSnapshot> _npcHealthBuffer = [];
    
    private readonly List<PlayerSnapshot> _playerSpawnBuffer = [];
    private readonly List<PlayerStateSnapshot> _playerStateBuffer = [];
    private readonly List<PlayerVitalsSnapshot> _playerVitalsBuffer = [];
    

    [Query]
    [All<PlayerControlled, NetworkId, DirtyFlags>]
    private void SyncPlayers(
        in Entity entity,
        in NetworkId networkId,
        ref DirtyFlags dirty)
    {
        if (dirty.IsEmpty) return;
        
        // Estado completo para spawns
        var dirtySnapshot = dirty.Snapshot();
        bool spawnRequested = dirtySnapshot == DirtyComponentType.All;

        if (spawnRequested)
        {
            _playerSpawnBuffer.Add(World.BuildPlayerDataSnapshot(entity).ToPlayerSpawnSnapshot());
            dirty.ClearDirtyMask(DirtyComponentType.All);
        }

        if (!spawnRequested)
        {
            // Estado de movimento
            if (dirty.IsDirty(DirtyComponentType.State))
                _playerStateBuffer.Add(World.BuildPlayerStateSnapshot(entity).ToPlayerStateSnapshot());

            // Vitals
            if (dirty.IsDirty(DirtyComponentType.Vitals))
                _playerVitalsBuffer.Add(World.BuildPlayerVitalsSnapshot(entity).ToPlayerVitalsSnapshot());

            // Combate (estado + resultado)
            if (dirty.IsDirty(DirtyComponentType.Combat) &&
                World.TryGet(entity, out CombatState combat))
            {
                var attackType = AttackType.Basic;
                var attackDuration = CombatLogic.GetAttackTypeSpeedMultiplier(attackType);

                if (World.TryGet(entity, out Attack attackAction))
                {
                    attackType = attackAction.Type;
                    attackDuration = attackAction.TotalDuration;
                }

                _combatBuffer.Add(new CombatStateSnapshot(
                    AttackerNetworkId: networkId.Value,
                    Type: attackType,
                    AttackDuration: attackDuration,
                    CooldownRemaining: combat.AttackCooldownTimer
                ));
            }
        }
        
        dirty.ClearAll();
    }
    
    [Query]
    [All<AIControlled, NetworkId, DirtyFlags>]
    private void SyncNpcs(
        in Entity entity,
        in NetworkId networkId,
        ref DirtyFlags dirty)
    {
        if (dirty.IsEmpty) return;
        
        // Estado completo para spawns
        bool spawnRequested = dirty.Snapshot() == DirtyComponentType.All;

        if (spawnRequested)
        {
            var npcData = World.BuildNpcData(entity).ToNpcSpawnData();
            _npcSpawnBuffer.Add(npcData);
            dirty.ClearDirtyMask(DirtyComponentType.All);
        }

        if (!spawnRequested && dirty.IsDirtyMask(DirtyComponentType.State))
        {
            var stateData = World
                .BuildNpcStateData(entity)
                .ToNpcStateSnapshot();
            _npcStateBuffer.Add(stateData);
        }
        
        if (!spawnRequested && dirty.IsDirtyMask(DirtyComponentType.Vitals))
        {
            var healthData = World
                .BuildNpcHealthData(entity)
                .ToNpcHealthSnapshot();
            _npcHealthBuffer.Add(healthData);
        }
        
        // Combate (estado + resultado)
        if (dirty.IsDirty(DirtyComponentType.Combat) &&
            World.TryGet(entity, out CombatState combat))
        {
            var attackType = AttackType.Basic;
            var attackDuration = CombatLogic.GetAttackTypeSpeedMultiplier(attackType);

            if (World.TryGet(entity, out Attack attackAction))
            {
                attackType = attackAction.Type;
                attackDuration = attackAction.TotalDuration;
            }

            _combatBuffer.Add(new CombatStateSnapshot(
                AttackerNetworkId: networkId.Value,
                Type: attackType,
                AttackDuration: attackDuration,
                CooldownRemaining: combat.AttackCooldownTimer
            ));
        }
        
        dirty.ClearAll();
    }
    
    public override void AfterUpdate(in float deltaTime)
    {
        FlushBuffers();
    }

    private void FlushBuffers()
    {
        // ===========================
        // ======= COMBAT ============
        // ===========================
        
        if (_combatBuffer.Count > 0)
        {
            var packet = new CombatStatePacket(_combatBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _combatBuffer.Clear();
        }
        
        // ===========================
        // ========== NPCs ===========
        // ===========================
            
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
        if (_npcHealthBuffer.Count > 0)
        {
            var packet = new NpcHealthPacket(_npcHealthBuffer.ToArray());
            sender.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.ReliableOrdered);
            _npcHealthBuffer.Clear();
        }
            
        // ===========================
        // ======== PLAYERS ==========
        // ===========================
            
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
    }
}