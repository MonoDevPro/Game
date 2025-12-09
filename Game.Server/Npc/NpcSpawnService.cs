using Game.DTOs.Game.Npc;
using Game.ECS.Entities;
using Game.Server.Simulation;

namespace Game.Server.Npc;

/// <summary>
/// Serviço responsável por criar NPCs no início da simulação e fornecer snapshots atuais.
/// </summary>
public sealed class NpcSpawnService(ServerGameSimulation simulation, INpcRepository repository, ILogger<NpcSpawnService> logger)
{
    public readonly record struct NpcInfo(string Name);

    private readonly Dictionary<int, NpcInfo> _activeNetworkIds = [];
    private int _nextNetworkId = 100_000;
    private bool _initialized;

    public IReadOnlyDictionary<int, NpcInfo> ActiveNetworkIds => _activeNetworkIds;

    public void SpawnInitialNpcs()
    {
        if (_initialized)
            return;

        var spawnPoints = repository.GetSpawnPoints(mapId: 1);

        foreach (var spawn in spawnPoints)
        {
            SpawnNpc(spawn.TemplateId, spawn.X, spawn.Y, spawn.Floor, spawn.MapId);
        }

        _initialized = true;
    }

    public void SpawnNpc(int templateId, int x, int y, sbyte floor, int mapId)
    {
        var template = repository.GetTemplate(templateId);
        var networkId = GenerateNetworkId();

        // Cria um template atualizado com a localização de spawn e networkId
        var npcSnapshot = new NpcData
        {
            NpcId = template.Id,
            NetworkId = networkId,
            Name = template.Name,
            Gender = (byte)template.Gender,
            Vocation = (byte)template.Vocation,
            DirX = (sbyte)template.DirX,
            DirY = (sbyte)template.DirY,
            MapId = mapId,
            Floor = floor,
            X = x,
            Y = y,
            MovementSpeed = template.MovementSpeed,
            AttackSpeed = template.AttackSpeed,
            PhysicalAttack = template.PhysicalAttack,
            MagicAttack = template.MagicAttack,
            PhysicalDefense = template.PhysicalDefense,
            MagicDefense = template.MagicDefense,
            Hp = template.CurrentHp,
            MaxHp = template.MaxHp,
            Mp = template.CurrentMp,
            MaxMp = template.MaxMp,
            HpRegen = template.HpRegen,
            MpRegen = template.MpRegen,
        };
        
        var behavior = new Behaviour(
            BehaviorType: template.BehaviorType,
            VisionRange: template.VisionRange,
            AttackRange: template.AttackRange,
            LeashRange: template.LeashRange,
            PatrolRadius: template.PatrolRadius,
            IdleDurationMin: template.IdleDurationMin,
            IdleDurationMax: template.IdleDurationMax
        );

        // Transforma Template em Entidade ECS
        var entity = simulation.CreateNpc(ref npcSnapshot, ref behavior);

        if (entity != Arch.Core.Entity.Null)
        {
            _activeNetworkIds.Add(networkId, new NpcInfo(template.Name));
            logger.LogInformation(
                "[NPC] Spawned NPC NetID={NetworkId} Name={Name} at ({X}, {Y}, {Z}) on map {MapId}",
                networkId,
                template.Name,
                x,
                y,
                floor,
                mapId);
        }
    }

    public IEnumerable<NpcData> BuildSnapshots()
    {
        foreach (var networkId in _activeNetworkIds)
            if (simulation.TryGetNpcEntity(networkId.Key, out var entity))
                yield return simulation.World.BuildNpcSnapshot(entity, networkId.Value.Name);
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

    private int GenerateNetworkId() => _nextNetworkId++;
}
