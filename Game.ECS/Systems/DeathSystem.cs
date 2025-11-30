using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
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
    [Query]
    [All<SpawnPoint, Respawning>]
    [None<Dead>]
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
    }

    [Query]
    [All<Health>]
    [None<Dead, Invulnerable>]
    private void DeathEntity(
        in Entity entity,
        in Health health)
    {
        if (health.Current > 0) 
            return;
        
        World.Add<Dead>(entity);
        logger?.LogInformation("[DeathSystem] Entity {Entity} has died.", entity);
    }
    
}