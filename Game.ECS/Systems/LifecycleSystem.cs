using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Events;
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
    [All<SpawnRequest, SpawnPoint>]
    private void ProcessSpawnRequest(
        in Entity entity,
        in SpawnPoint spawnPoint,
        ref SpawnRequest _,
        ref MapId mapId,
        ref Position position)
    {
        mapId.Value = spawnPoint.MapId;
        position.X = spawnPoint.X;
        position.Y = spawnPoint.Y;
        position.Z = spawnPoint.Z;
        
        var spawnEvent = new SpawnEvent(entity, spawnPoint, World.Get<NetworkId>(entity).Value);
        EventBus.Send(ref spawnEvent);
        
        World.Remove<SpawnRequest>(entity);
        
        logger?.LogInformation("[LifecycleSystem] Entity {Entity} has spawned at Map {MapId} ({X}, {Y}, Floor {Floor}).",
            entity, spawnPoint.MapId, spawnPoint.X, spawnPoint.Y, spawnPoint.Z);
    }

    [Query]
    [All<SpawnPoint, Respawning, Dead>]
    private void RespawnEntity(
        in Entity entity,
        in SpawnPoint spawnPoint,
        ref Respawning respawning,
        ref MapId mapId,
        ref Position position,
        [Data] float deltaTime)
    {
        respawning.TimeRemaining -= deltaTime;
        if (respawning.TimeRemaining > 0f)
            return;
        
        // Respawn completo
        RestoreVitalsOnRespawn(entity);
        CleanupTransientState(entity);

        mapId.Value = spawnPoint.MapId;
        position.X = spawnPoint.X;
        position.Y = spawnPoint.Y;
        position.Z = spawnPoint.Z;
        
        World.Remove<Dead, Respawning>(entity);

        // Notifica outros sistemas (teleport/respawn)
        if (World.Has<NetworkId>(entity))
        {
            var spawnEvent = new SpawnEvent(entity, spawnPoint, World.Get<NetworkId>(entity).Value);
            EventBus.Send(ref spawnEvent);
        }
        
        LogDebug("[LifecycleSystem] Entity {Entity} has respawned at Map {MapId} ({X}, {Y}, Floor {Floor}).",
            entity, spawnPoint.MapId, spawnPoint.X, spawnPoint.Y, spawnPoint.Z);
    }

    [Query]
    [All<Health>]
    [None<Dead, Invulnerable, Respawning>]
    private void DeathEntity(
        in Entity entity,
        ref Health health)
    {
        if (health.Current > 0) 
            return;

        if (health.Current < 0)
            health.Current = 0;

        CleanupTransientState(entity);
        
        World.Add<Respawning, Dead>(entity, new Respawning
        {
            TimeRemaining = SimulationConfig.DefaultRespawnTime,
            TotalTime = SimulationConfig.DefaultRespawnTime
        });
        
        var deathEvent = new DeathEvent(entity, Entity.Null, World.Get<Position>(entity));
        EventBus.Send(ref deathEvent);
        
        logger?.LogInformation("[DeathSystem] Entity {Entity} has died.", entity);
    }

    private void CleanupTransientState(in Entity entity)
    {
        if (World.Has<Speed>(entity))
        {
            ref var speed = ref World.Get<Speed>(entity);
            speed.Value = 0f;
        }

        if (World.Has<CombatState>(entity))
        {
            ref var state = ref World.Get<CombatState>(entity);
            state.InCooldown = false;
            state.CooldownTimer = 0f;
        }

        if (World.Has<AttackCommand>(entity))
            World.Remove<AttackCommand>(entity);

        if (World.Has<MovementIntent>(entity))
            World.Remove<MovementIntent>(entity);

        if (World.Has<MovementApproved>(entity))
            World.Remove<MovementApproved>(entity);

        if (World.Has<MovementBlocked>(entity))
            World.Remove<MovementBlocked>(entity);
    }

    private void RestoreVitalsOnRespawn(in Entity entity)
    {
        if (World.Has<Health>(entity))
        {
            ref var health = ref World.Get<Health>(entity);
            var oldHp = health.Current;

            var reviveHp = 0;
            if (health.Max > 0)
            {
                reviveHp = (int)MathF.Ceiling(health.Max * SimulationConfig.ReviveHealthPercent);
                reviveHp = Math.Clamp(reviveHp, 1, health.Max);
            }

            health.Current = reviveHp;
            health.AccumulatedRegeneration = 0f;

            if (oldHp != health.Current)
            {
                var evt = new HealthChangedEvent(entity, oldHp, health.Current, health.Max);
                EventBus.Send(ref evt);
            }
        }

        if (World.Has<Mana>(entity))
        {
            ref var mana = ref World.Get<Mana>(entity);
            var oldMp = mana.Current;

            var reviveMp = 0;
            if (mana.Max > 0)
            {
                reviveMp = (int)MathF.Ceiling(mana.Max * SimulationConfig.ReviveManaPercent);
                reviveMp = Math.Clamp(reviveMp, 0, mana.Max);
            }

            mana.Current = reviveMp;
            mana.AccumulatedRegeneration = 0f;

            if (oldMp != mana.Current)
            {
                var evt = new ManaChangedEvent(entity, oldMp, mana.Current, mana.Max);
                EventBus.Send(ref evt);
            }
        }
    }
}