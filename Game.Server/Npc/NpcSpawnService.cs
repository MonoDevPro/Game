using Game.ECS.Server;
using Game.ECS.Shared.Components.Entities;
using Game.ECS.Shared.Components.Navigation;

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

        var spawnedAny = false;

        // TODO: Implementar spawn baseado em configuração externa
        /*foreach (var mapId in simulation.RegisteredMaps)
        {
            var spawnedOnMap = false;

            foreach (var spawn in repository.GetSpawnPoints(mapId))
            {
                SpawnNpc(spawn.TemplateId, spawn.X, spawn.Y, spawn.Floor, spawn.MapId);
                spawnedOnMap = true;
                spawnedAny = true;
            }

            if (!spawnedOnMap)
            {
                logger.LogInformation("[NPC] No spawn points configured for map {MapId}", mapId);
            }
        }*/

        if (!spawnedAny)
        {
            logger.LogWarning("[NPC] No NPC spawn points found for any registered map. Skipping spawn.");
        }

        _initialized = true;
    }

    public void SpawnNpc(int id, int x, int y, sbyte floor, int mapId)
    {
        var template = repository.GetTemplate(id);
        var networkId = GenerateNetworkId();

        // Cria um template atualizado com a localização de spawn e networkId
        var npcSnapshot = new ECS.Shared.Core.Entities.NpcData
        {
            Id = networkId,
            Name = template.Name,
            Direction = MovementDirection.South,
            X = x,
            Y = y,
            Z = floor,
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
        };

        var behavior = new AIBehaviour
        {
            Type = template.BehaviorType,
            VisionRange = template.VisionRange,
            AttackRange = template.AttackRange,
            LeashRange = template.LeashRange,
            PatrolRadius = template.PatrolRadius,
            IdleDurationMin = template.IdleDurationMin,
            IdleDurationMax = template.IdleDurationMax
        };

        // Transforma Template em Entidade ECS
        var entity = simulation.CreateNpcEntity(ref npcSnapshot);

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

    public IEnumerable<ECS.Shared.Core.Entities.NpcData> BuildSnapshots()
    {
        foreach (var networkId in _activeNetworkIds)
            if (simulation.TryGetNpcEntity(networkId.Key, out var entity))
                yield return new ECS.Shared.Core.Entities.NpcData(); // TODO: Preencher snapshot a partir da entidade
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
