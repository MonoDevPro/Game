using Game.Domain.Enums;
using Game.Domain.Templates;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;
using Game.ECS.Entities.Npc;
using Game.ECS.Schema.Components;
using Game.Server.ECS;

namespace Game.Server.Npc;

/// <summary>
/// Serviço responsável por criar NPCs no início da simulação e fornecer snapshots atuais.
/// </summary>
public sealed class NpcSpawnService(ServerGameSimulation simulation, INpcRepository repository, ILogger<NpcSpawnService> logger)
{
    public readonly record struct NpcInfo(string Name);
    
    private readonly INpcRepository _repository = repository;
    private readonly Dictionary<int, NpcInfo> _activeNetworkIds = [];
    private int _nextNetworkId = 100_000;
    private bool _initialized;

    public IReadOnlyDictionary<int, NpcInfo> ActiveNetworkIds => _activeNetworkIds;

    public void SpawnInitialNpcs()
    {
        if (_initialized)
            return;

        // O serviço pede ao repositório: "Onde devo criar monstros?"
        var spawnPoints = _repository.GetSpawnPoints(mapId: 0); 

        foreach (var spawn in spawnPoints)
        {
            SpawnNpc(spawn.TemplateId, new Position(spawn.X, spawn.Y), spawn.Floor, spawn.MapId);
        }

        _initialized = true;
    }

    public void SpawnNpc(string templateId, Position position, int floor, int mapId)
    {
        var template = _repository.GetTemplate(templateId);
        var networkId = GenerateNetworkId();
        
        // Transforma Template em Entidade ECS
        var entity = simulation.CreateNpcFromTemplate(template, position, floor, mapId, networkId);
        
        if (entity != Arch.Core.Entity.Null)
        {
            _activeNetworkIds.Add(networkId, new NpcInfo(template.Name));
            logger.LogInformation(
                "[NPC] Spawned NPC NetID={NetworkId} Name={Name} at ({X}, {Y}, {Z}) on map {MapId}",
                networkId,
                template.Name,
                position.X,
                position.Y,
                floor,
                mapId);
        }
    }

    public IEnumerable<NpcSnapshot> BuildSnapshots()
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

    private int GenerateNetworkId() => _nextNetworkId++;
}
