using Arch.Core;
using Arch.Relationships;
using Arch.System;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Shared;
using Simulation.Core.ECS.Shared.Utils.Map;
using Simulation.Core.Network;

namespace Simulation.Core.ECS.Server.Systems;

public sealed partial class EntitySnapshotSystem(World world, EntityIndexSystem index, ILogger<EntitySnapshotSystem> logger)
    : BaseSystem<World, float>(world)
{
    // Reuso de listas (apenas no thread principal)
    private readonly List<Entity> _entities = new();
    private readonly List<PlayerSnapshot> _players = new();
    
    [Query]
    [All<NewlyCreated, PlayerId, MapId>]
    private void ProcessJoin(in Entity playerEntity, in PlayerId pId, in MapId mapId)
    {
        if (!index.TryGetMap(mapId.Value, out var map) || !World.IsAlive(map.Entity))
        {
            logger.LogWarning("CharId {CharId} tentou entrar no mapa {MapId}, mas o mapa não está carregado no mundo. O login será descartado.", pId.Value, mapId.Value);
            World.Remove<NewlyCreated>(playerEntity);
            World.Add<NewlyDestroyed>(playerEntity, new NewlyDestroyed());
            return;
        }
        
        var snapshot = ReadMapSnapshot(pId.Value, map);
    }
    
    private MapSnapshot ReadMapSnapshot(int playerId, (Entity, MapService) map)
    {
        var mapEntitiesList = _entities;
        mapEntitiesList.Clear();
        var others = _players;
        others.Clear();
        
        var (mapEntity, mapService) = map;
        
        ref var relationshipsFromMap = ref World.GetRelationships<PlayerId>(mapEntity);
        foreach (var rel in relationshipsFromMap)
        {
            if (World.IsAlive(rel.Key))
                mapEntitiesList.Add(rel.Key);
        }
        foreach (var ent in mapEntitiesList)
            others.Add(ReadPlayerSnapshot(ent));
        
        return new MapSnapshot
        {
            PlayerId = playerId,
            MapId = mapService.MapId,
            Width = mapService.Width,
            Height = mapService.Height,
            Tiles = mapService.Tiles,
            Players = others.ToArray()
        };
    }
    
    private PlayerSnapshot ReadPlayerSnapshot(Entity e)
    {
        ref var id = ref World.Get<PlayerId>(e);
        ref var playerInfo = ref World.Get<PlayerInfo>(e);
        ref var pos = ref World.Get<Position>(e);
        ref var dir = ref World.Get<Direction>(e);
        ref var attack = ref World.Get<AttackStats>(e);
        ref var move = ref World.Get<MoveStats>(e);
        ref var health = ref World.Get<Health>(e);

        return new PlayerSnapshot
        {
            PlayerId = id.Value,
            Name = playerInfo.Name,
            Gender = playerInfo.Gender,
            Vocation = playerInfo.Vocation,
            Position = pos,
            Direction = dir,
            Health = health,
            MoveStats = move,
            AttackStats = attack
        };
    }
}
