using System.Collections.Generic;
using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Factories;
using Game.Server.ECS;
using Microsoft.Extensions.Logging;

namespace Game.Server.Npc;

/// <summary>
/// Serviço responsável por criar NPCs no início da simulação e fornecer snapshots atuais.
/// </summary>
public sealed class NpcSpawnService(ServerGameSimulation simulation, ILogger<NpcSpawnService> logger)
{
    private readonly List<NpcSpawnDefinition> _definitions = BuildDefaultDefinitions();
    private readonly List<int> _activeNetworkIds = new();
    private int _nextNetworkId = 100_000;
    private bool _initialized;

    public IReadOnlyList<int> ActiveNetworkIds => _activeNetworkIds;

    public void SpawnInitialNpcs()
    {
        if (_initialized)
            return;

        foreach (var definition in _definitions)
        {
            var npcData = BuildNpcData(definition);
            var entity = simulation.CreateNpc(npcData);
            ConfigureAiComponents(entity, npcData, definition);
            _activeNetworkIds.Add(npcData.NetworkId);
            logger.LogInformation(
                "[NPC] Spawned NPC NetID={NetworkId} at ({X}, {Y}, {Z}) on map {MapId}",
                npcData.NetworkId,
                npcData.PositionX,
                npcData.PositionY,
                npcData.PositionZ,
                npcData.MapId);
        }

        _initialized = true;
    }

    public IEnumerable<NPCData> BuildSnapshots()
    {
        foreach (var networkId in _activeNetworkIds)
        {
            if (simulation.TryGetNpcEntity(networkId, out var entity))
                yield return simulation.World.BuildNPCSnapshot(entity);
        }
    }

    public bool TryDespawnNpc(int networkId)
    {
        if (!_activeNetworkIds.Contains(networkId))
            return false;

        if (!simulation.DestroyNpc(networkId))
            return false;

        _activeNetworkIds.Remove(networkId);
        logger.LogInformation("[NPC] Despawned NPC NetID={NetworkId}", networkId);
        return true;
    }

    private NPCData BuildNpcData(NpcSpawnDefinition definition)
    {
        return new NPCData
        {
            NetworkId = GenerateNetworkId(),
            MapId = definition.MapId,
            PositionX = definition.PositionX,
            PositionY = definition.PositionY,
            PositionZ = definition.PositionZ,
            Hp = definition.Hp,
            MaxHp = definition.MaxHp,
            HpRegen = definition.HpRegen,
            PhysicalAttack = definition.PhysicalAttack,
            MagicAttack = definition.MagicAttack,
            PhysicalDefense = definition.PhysicalDefense,
            MagicDefense = definition.MagicDefense
        };
    }

    private void ConfigureAiComponents(Entity entity, in NPCData npcData, in NpcSpawnDefinition definition)
    {
        ref var behavior = ref simulation.World.Get<NpcBehavior>(entity);
        behavior.Type = definition.BehaviorType;
        behavior.VisionRange = definition.VisionRange;
        behavior.AttackRange = definition.AttackRange;
        behavior.LeashRange = definition.LeashRange;
        behavior.PatrolRadius = definition.PatrolRadius;
        behavior.IdleDurationMin = definition.IdleDurationMin;
        behavior.IdleDurationMax = definition.IdleDurationMax;

        var homePosition = new Position
        {
            X = npcData.PositionX,
            Y = npcData.PositionY,
            Z = npcData.PositionZ
        };

        ref var patrol = ref simulation.World.Get<NpcPatrol>(entity);
        patrol.HomePosition = homePosition;
        patrol.Destination = homePosition;
        patrol.Radius = definition.PatrolRadius;
        patrol.HasDestination = false;

        ref var target = ref simulation.World.Get<NpcTarget>(entity);
        target.LastKnownPosition = homePosition;
    }

    private int GenerateNetworkId() => _nextNetworkId++;

    private static List<NpcSpawnDefinition> BuildDefaultDefinitions() =>
        [
            new NpcSpawnDefinition
            {
                MapId = 0,
                PositionX = 10,
                PositionY = 10,
                PositionZ = 0,
                Hp = 150,
                MaxHp = 150,
                HpRegen = 0.5f,
                PhysicalAttack = 25,
                MagicAttack = 5,
                PhysicalDefense = 10,
                MagicDefense = 5,
                BehaviorType = NpcBehaviorType.Aggressive,
                VisionRange = 8f,
                AttackRange = 1.5f,
                LeashRange = 14f,
                PatrolRadius = 2f,
                IdleDurationMin = 0.75f,
                IdleDurationMax = 1.5f
            },
            new NpcSpawnDefinition
            {
                MapId = 0,
                PositionX = 20,
                PositionY = 20,
                PositionZ = 0,
                Hp = 120,
                MaxHp = 120,
                HpRegen = 0.3f,
                PhysicalAttack = 18,
                MagicAttack = 8,
                PhysicalDefense = 8,
                MagicDefense = 6,
                BehaviorType = NpcBehaviorType.Defensive,
                VisionRange = 5f,
                AttackRange = 1.2f,
                LeashRange = 10f,
                PatrolRadius = 4f,
                IdleDurationMin = 1.5f,
                IdleDurationMax = 3.5f
            }
        ];

    private sealed class NpcSpawnDefinition
    {
        public int MapId { get; init; }
        public int PositionX { get; init; }
        public int PositionY { get; init; }
        public int PositionZ { get; init; }
        public int Hp { get; init; }
        public int MaxHp { get; init; }
        public float HpRegen { get; init; }
        public int PhysicalAttack { get; init; }
        public int MagicAttack { get; init; }
        public int PhysicalDefense { get; init; }
        public int MagicDefense { get; init; }
        public NpcBehaviorType BehaviorType { get; init; } = NpcBehaviorType.Aggressive;
        public float VisionRange { get; init; } = 6f;
        public float AttackRange { get; init; } = 1.25f;
        public float LeashRange { get; init; } = 12f;
        public float PatrolRadius { get; init; } = 0f;
        public float IdleDurationMin { get; init; } = 1.5f;
        public float IdleDurationMax { get; init; } = 3.5f;
    }
}
