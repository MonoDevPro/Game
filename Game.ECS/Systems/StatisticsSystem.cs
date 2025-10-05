using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;

namespace Game.ECS.Systems;


public sealed partial class StatisticsSystem(World world, IPlayerStatsService statsService) : GameSystem(world)
{
    private List<(Guid playerId, RuntimeStats stats)> entitiesToPersist = [];
    
    [Query]
    [All<Position, Velocity>]
    private void MoveEntity(Entity entity, ref PlayerId playerId, ref RuntimeStats stats)
    {
        if (stats.NeedsPersistence)
        {
            entitiesToPersist.Add((playerId.Value, stats));
            stats.NeedsPersistence = false;
        }
                
        World.Remove<StatsDirty>(entity);
        
        // Persistir estatísticas em batch (async)
        if (entitiesToPersist.Count > 0)
        {
            _ = statsService.UpdateBatchAsync(entitiesToPersist);
        }
    }
    
    public void RecordKill(Entity entity, bool isPvP)
    {
        if (!World.TryGet(entity, out RuntimeStats stats))
            return;

        stats.SessionKills++;
        stats.CurrentKillStreak++;
        stats.LastKillTime = Time.GetTime();
        stats.NeedsPersistence = true;

        World.Set(entity, stats);
        World.Add<StatsDirty>(entity);
    }

    public void RecordDeath(Entity entity)
    {
        if (!World.TryGet(entity, out RuntimeStats stats))
            return;

        stats.SessionDeaths++;
        stats.CurrentKillStreak = 0; // Reset kill streak
        stats.NeedsPersistence = true;

        World.Set(entity, stats);
        World.Add<StatsDirty>(entity);
    }

    public void RecordDamage(Entity entity, int damage, bool isCritical)
    {
        if (!World.TryGet(entity, out RuntimeStats stats))
            return;

        stats.SessionDamageDealt += damage;
            
        if (isCritical && damage > stats.LastCriticalDamage)
        {
            stats.LastCriticalDamage = damage;
        }
            
        stats.NeedsPersistence = true;

        World.Set(entity, stats);
        World.Add<StatsDirty>(entity);
    }

    public void RecordMovement(Entity entity, float distance)
    {
        if (!World.TryGet(entity, out RuntimeStats stats))
            return;

        stats.SessionDistanceTraveled += distance;
        stats.NeedsPersistence = true;

        World.Set(entity, stats);
    }
}