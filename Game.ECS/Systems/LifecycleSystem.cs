using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Schema;
using Game.ECS.Schema.Components;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por processar mortes de entidades.
/// Verifica entidades com Health e marca como Dead se necessário.
/// </summary>
public sealed partial class LifecycleSystem(World world, ILogger<LifecycleSystem>? logger = null)
    : GameSystem(world)
{
    private const float RespawnTimeSeconds = 5f;

    [Query]
    [All<SpawnRequest, SpawnPoint>]
    private void ProcessSpawnRequest(
        in Entity entity,
        in SpawnPoint spawnPoint,
        ref SpawnRequest _,
        ref MapId mapId,
        ref Floor floor,
        ref Position position)
    {
        mapId.Value = spawnPoint.MapId;
        floor.Value = spawnPoint.Floor;
        position.X = spawnPoint.X;
        position.Y = spawnPoint.Y;
        
        var spawnEvent = new SpawnEvent(entity, spawnPoint, World.Get<NetworkId>(entity).Value);
        EventBus.Send(ref spawnEvent);
        
        World.Remove<SpawnRequest>(entity);
        
        logger?.LogInformation("[LifecycleSystem] Entity {Entity} has spawned at Map {MapId} ({X}, {Y}, Floor {Floor}).",
            entity, spawnPoint.MapId, spawnPoint.X, spawnPoint.Y, spawnPoint.Floor);
    }

    [Query]
    [All<SpawnPoint, Respawning, Dead>]
    private void RespawnEntity(
        in Entity entity,
        in SpawnPoint spawnPoint,
        ref Respawning respawning,
        ref MapId mapId,
        ref Floor floor,
        ref Position position,
        [Data] float deltaTime)
    {
        respawning.TimeRemaining -= deltaTime;
        if (respawning.TimeRemaining > 0f)
            return;
        
        // Respawn completo
        mapId.Value = spawnPoint.MapId;
        floor.Value = spawnPoint.Floor;
        position.X = spawnPoint.X;
        position.Y = spawnPoint.Y;
        
        World.Remove<Dead, Respawning>(entity);
        
        LogDebug("[LifecycleSystem] Entity {Entity} has respawned at Map {MapId} ({X}, {Y}, Floor {Floor}).",
            entity, spawnPoint.MapId, spawnPoint.X, spawnPoint.Y, spawnPoint.Floor);
    }

    [Query]
    [All<Health>]
    [None<Dead, Invulnerable, Respawning>]
    private void DeathEntity(
        in Entity entity,
        in Health health)
    {
        if (health.Current > 0) 
            return;
        
        World.Add<Respawning, Dead>(entity, new Respawning
        {
            TimeRemaining = RespawnTimeSeconds,
            TotalTime = RespawnTimeSeconds
        });
        
        var deathEvent = new DeathEvent(entity, Entity.Null, World.Get<Position>(entity));
        EventBus.Send(ref deathEvent);
        
        logger?.LogInformation("[DeathSystem] Entity {Entity} has died.", entity);
    }
}