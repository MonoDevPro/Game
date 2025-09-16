using Arch.Core;
using Arch.Relationships;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.ECS.Indexes.Player;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Staging.Map;
using Simulation.Core.ECS.Staging.Player;

namespace Simulation.Core.ECS.Systems;

/// <summary>
/// Sistema central responsável por manter índices de alta performance
/// para as entidades principais do mundo, como Mapas e Jogadores.
/// </summary>
 [PipelineSystem(SystemStage.Logic, -50)] // cedo na fase lógica
public sealed partial class EntityIndexSystem(World world) : 
    BaseSystem<World, float>(world), 
    IPlayerIndex,
    IMapIndex
{
    // Índices privados que oferecem busca O(1)
    private readonly Dictionary<int, MapInstance> _mapsByMapId = new();
    private readonly Dictionary<int, Entity> _playersByCharId = new();
    
    // Cache para evitar alocações a cada chamada do GetPlayerIdsInMap
    private readonly List<int> _playerIdCache = [];
    
    // --- Ciclo de Vida do Índice ---

    [Query]
    [All<NewlyCreated, MapId, MapData>] // Procura por MapData, não MapService
    private void IndexNewMaps(in Entity entity, ref MapId mapId, ref MapData mapData)
    {
        var mapService = MapService.CreateFromTemplate(mapData);
        World.Add(entity, mapService);
        _mapsByMapId[mapId.Value] = new MapInstance(entity, mapService);
    }


    [Query]
    [All<NewlyCreated, PlayerId, PlayerData>]
    private void IndexNewPlayers(in Entity entity, ref PlayerId playerId, ref MapId mapId)
    {
        _playersByCharId[playerId.Value] = entity;
        
        // Cria uma relação do Mapa (pai) para o Jogador (filho), com o PlayerId como payload.
        if (_mapsByMapId.TryGetValue(mapId.Value, out var mapInfo))
            World.AddPlayerToMap<PlayerId>(entity, mapInfo, playerId);
    }
    
    [Query]
    [All<NewlyDestroyed, MapId>]
    private void UnindexDestroyedMaps(in Entity entity, ref MapId mapId)
    {
        if (!_mapsByMapId.TryGetValue(mapId.Value, out var mapInfo))
            return;
        
        if (mapInfo.Entity == entity)
            _mapsByMapId.Remove(mapId.Value);
    }

    [Query]
    [All<NewlyDestroyed, PlayerId>]
    private void UnindexDestroyedPlayers(in Entity entity, ref PlayerId playerId)
    {
        _playersByCharId.Remove(playerId.Value);
        // Nota: O Arch.Relationships remove automaticamente as relações quando uma das entidades é destruída.
    }

    // --- API Pública do Índice ---

    /// <summary>
    /// Obtém a entidade e o serviço de um mapa pelo seu ID, se existir e estiver vivo.
    /// </summary>
    public bool TryGetMap(int mapId, out MapInstance mapInstance)
    {
        if (_mapsByMapId.TryGetValue(mapId, out mapInstance))
        {
            if (World.IsAlive(mapInstance.Entity))
            {
                return true;
            }
            _mapsByMapId.Remove(mapId); // Auto-correção
        }
        mapInstance = default;
        return false;
    }


    /// <summary>
    /// Obtém a entidade de um jogador pelo seu ID, se existir e estiver viva.
    /// </summary>
    public bool TryGetPlayerEntity(int charId, out Entity entity)
    {
        if (_playersByCharId.TryGetValue(charId, out entity))
        {
            if (World.IsAlive(entity))
            {
                return true;
            }
            _playersByCharId.Remove(charId); // Auto-correção
        }
        entity = default;
        return false;
    }

    /// <summary>
    /// Obtém uma coleção de todos os IDs de jogadores presentes num determinado mapa.
    /// Utiliza as relações do ArchECS para uma consulta eficiente.
    /// </summary>
    public IEnumerable<int> GetPlayerIdsInMap(int mapId)
    {
        _playerIdCache.Clear();

        if (!TryGetMap(mapId, out var mapInstance) || !World.HasRelationship<PlayerId>(mapInstance.Entity)) 
            return _playerIdCache;
        
        return World.GetPlayerIdsInMap(mapInstance, _playerIdCache);
    }
}