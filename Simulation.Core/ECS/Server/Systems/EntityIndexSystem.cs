using Arch.Core;
using Arch.Relationships;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Shared;
using Simulation.Core.ECS.Shared.Utils.Map;

namespace Simulation.Core.ECS.Server.Systems;

/// <summary>
/// Sistema central responsável por manter índices de alta performance
/// para as entidades principais do mundo, como Mapas e Jogadores.
/// </summary>
public sealed partial class EntityIndexSystem(World world) : BaseSystem<World, float>(world)
{
    // Índices privados que oferecem busca O(1)
    private readonly Dictionary<int, (Entity Entity, MapService MapService)> _mapsByMapId = new();
    private readonly Dictionary<int, Entity> _playersByCharId = new();
    
    // --- Ciclo de Vida do Índice ---

    [Query]
    [All<NewlyCreated, MapId, MapService>]
    private void IndexNewMaps(in Entity entity, ref MapId mapId, ref MapService mapService)
    {
        _mapsByMapId[mapId.Value] = (entity, mapService);
        World.Remove<MapService>(entity);
    }

    [Query]
    [All<NewlyCreated, PlayerId, MapId>]
    private void IndexNewPlayers(in Entity entity, ref PlayerId playerId, ref MapId mapId)
    {
        _playersByCharId[playerId.Value] = entity;
        
        // Cria uma relação do Mapa (pai) para o Jogador (filho), com o PlayerId como payload.
        if (_mapsByMapId.TryGetValue(mapId.Value, out var mapInfo))
            World.AddRelationship<PlayerId>(entity, mapInfo.Entity, playerId);
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
    public bool TryGetMap(int mapId, out (Entity Entity, MapService MapService) mapInfo)
    {
        if (_mapsByMapId.TryGetValue(mapId, out mapInfo))
        {
            if (World.IsAlive(mapInfo.Entity))
            {
                return true;
            }
            _mapsByMapId.Remove(mapId); // Auto-correção
        }
        mapInfo = default;
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
}