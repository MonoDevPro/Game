using Arch.Core;
using Arch.System;
using Arch.Relationships;
using Arch.System.SourceGenerator;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using Simulation.Core.Server.Snapshot;
using Simulation.Core.Shared.Templates;
using Simulation.Core.Shared.Components;

namespace Simulation.Core.Server.Systems;

public sealed partial class EntitySnapshotSystem(World world, EntityIndexSystem index, IPlayerSnapshotArea snapshotArea, ILogger<EntitySnapshotSystem> logger)
    : BaseSystem<World, float>(world), IPlayerSnapshotArea
{
    private readonly ConcurrentQueue<MapSnapshotPacket> _queue = new();
    
    public void StageJoinGameSnapshot(MapSnapshotPacket snapshot)
    {
        _queue.Enqueue(snapshot);
    }

    public bool TryDequeueJoinGameSnapshot(out MapSnapshotPacket snapshot)
    {
        snapshot = default;
        if (!_queue.TryDequeue(out var item)) 
            return false;
        
        snapshot = item;
        return true;
    }
    
    // Reuso de listas (apenas no thread principal)
    private readonly List<Entity> _entities = new();
    private readonly List<PlayerSnapshot> _players = new();
    [Query]
    [All<NewlyCreated, PlayerData>]
    private void ProcessJoin(in Entity playerEntity, in PlayerId pId, in MapId mapId)
    {
        var mapEntitiesList = _entities;
        mapEntitiesList.Clear();
        var others = _players;
        others.Clear();
        
        if (!index.TryGetMap(mapId.Value, out var mapEntity) || !World.IsAlive(mapEntity.Entity))
        {
            logger.LogWarning("CharId {CharId} tentou entrar no mapa {MapId}, mas o mapa não está carregado no mundo. O login será descartado.", pId.Value, mapId.Value);
            World.Destroy(playerEntity);
            return;
        }
        
        ref var relationshipsFromMap = ref World.GetRelationships<PlayerId>(mapEntity.Entity);
        foreach (var rel in relationshipsFromMap)
        {
            if (World.IsAlive(rel.Key))
                mapEntitiesList.Add(rel.Key);
        }
        foreach (var ent in mapEntitiesList)
            others.Add(BuildPlayerSnapshot(ent));
        
        var snapshot = new MapSnapshotPacket
        {
            PlayerId = pId.Value,
            MapId = mapId.Value,
            Width = mapEntity.MapService.Width,
            Height = mapEntity.MapService.Height,
            Tiles = mapEntity.MapService.Tiles,
            Players = others.ToArray()
        };
        snapshotArea.StageJoinGameSnapshot(snapshot);
    }
    
    private PlayerSnapshot BuildPlayerSnapshot(Entity e)
    {
        ref var id = ref World.Get<PlayerId>(e);
        ref var pos = ref World.Get<Position>(e);
        ref var dir = ref World.Get<Direction>(e);
        ref var attack = ref World.Get<AttackStats>(e);
        ref var move = ref World.Get<MoveStats>(e);
        ref var health = ref World.Get<Health>(e);
        
        var stored = World.Get<PlayerData>(e);

        return new PlayerSnapshot
        {
            PlayerId = id.Value,
            Name = stored.Name,
            Gender = stored.Gender,
            Vocation = stored.Vocation,
            Position = pos,
            Direction = dir,
            Health = health,
            MoveStats = move,
            AttackStats = attack
        };
    }
}
