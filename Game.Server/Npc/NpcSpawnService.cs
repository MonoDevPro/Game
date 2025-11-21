using Game.Domain.Enums;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Factories;
using Game.Server.ECS;

namespace Game.Server.Npc;

/// <summary>
/// Serviço responsável por criar NPCs no início da simulação e fornecer snapshots atuais.
/// </summary>
public sealed class NpcSpawnService(ServerGameSimulation simulation, ILogger<NpcSpawnService> logger)
{
    public readonly record struct NpcInfo(string Name);
    
    private readonly List<NpcSpawnDefinition> _definitions = BuildDefaultDefinitions();
    private readonly Dictionary<int, NpcInfo> _activeNetworkIds = [];
    private int _nextNetworkId = 100_000;
    private bool _initialized;

    public IReadOnlyDictionary<int, NpcInfo> ActiveNetworkIds => _activeNetworkIds;

    public void SpawnInitialNpcs()
    {
        if (_initialized)
            return;

        foreach (var definition in _definitions)
        {
            var npcData = BuildNpcData(definition);
            simulation.CreateNpc(npcData, definition.Behavior);
            _activeNetworkIds.Add(npcData.NetworkId, new NpcInfo(definition.Name));
            logger.LogInformation(
                "[NPC] Spawned NPC NetID={NetworkId} Name={Name} at ({X}, {Y}, {Z}) on map {MapId}",
                npcData.NetworkId,
                npcData.Name,
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
            if (simulation.TryGetNpcEntity(networkId.Key, out var entity))
                yield return simulation.World.BuildNpcData(entity) with
                {
                    Name = networkId.Value.Name
                };
        }
    }

    public bool TryDespawnNpc(int networkId)
    {
        if (!_activeNetworkIds.ContainsKey(networkId))
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
            Name = definition.Name,
            Gender = definition.Gender,
            Vocation = definition.Vocation,
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
    [
        new()
        {
            MapId = 0,
            Name = "Orc Warrior",
            Gender = (byte)Gender.Male,
            Vocation = (byte)VocationType.Warrior,
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
            Behavior = new NpcBehaviorData(
                BehaviorType: (byte)NpcBehaviorType.Aggressive,
                VisionRange: 8f,
                AttackRange: 1.5f,
                LeashRange: 10f,
                PatrolRadius: 0f,
                IdleDurationMin: 0f,
                IdleDurationMax: 0f)
        },
        new()
        {
            MapId = 0,
            Name = "Goblin",
            Gender = (byte)Gender.Male,
            Vocation = (byte)VocationType.Archer,
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
            Behavior = new NpcBehaviorData(
                BehaviorType: (byte)NpcBehaviorType.Defensive,
                VisionRange: 6f,
                AttackRange: 5f,
                LeashRange: 12f,
                PatrolRadius: 0f,
                IdleDurationMin: 0f,
                IdleDurationMax: 0f)
        }
    ];

    private sealed class NpcSpawnDefinition
    {
        public int MapId { get; init; }
        public string Name { get; init; } = "NPC";
        public byte Gender { get; init; }
        public byte Vocation { get; init; }
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
        public NpcBehaviorData Behavior { get; init; }
    }
}