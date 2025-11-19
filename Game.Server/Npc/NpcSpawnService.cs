using System.Collections.Generic;
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
            simulation.CreateNpc(npcData);
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

    private int GenerateNetworkId() => _nextNetworkId++;

    private static List<NpcSpawnDefinition> BuildDefaultDefinitions() =>
        new()
        {
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
                MagicDefense = 5
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
                MagicDefense = 6
            }
        };

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
    }
}
