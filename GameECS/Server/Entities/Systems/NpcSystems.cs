using Arch.Core;
using Game.Domain.AI.Enums;
using Game.Domain.AI.ValueObjects;
using Game.Domain.Enums;
using Game.Domain.ValueObjects.Combat;
using Game.Domain.ValueObjects.Identitys;
using Game.Domain.ValueObjects.Map;
using Game.Domain.ValueObjects.Vitals;

namespace GameECS.Server.Entities.Systems;

/// <summary>
/// Sistema de IA para NPCs.
/// </summary>
public sealed class NpcAISystem : IDisposable
{
    private readonly World _world;
    private readonly Random _random = new();
    private readonly QueryDescription _idleQuery;
    private readonly QueryDescription _chasingQuery;
    private readonly QueryDescription _returningQuery;

    public NpcAISystem(World world)
    {
        _world = world;
        _idleQuery = new QueryDescription()
            .WithAll<NpcAI, NpcBehavior, GridPosition>()
            .WithNone<Dead>();
        _chasingQuery = new QueryDescription()
            .WithAll<NpcAI, NpcBehavior, GridPosition, AggroTable>()
            .WithNone<Dead>();
        _returningQuery = new QueryDescription()
            .WithAll<NpcAI, SpawnInfo, GridPosition>()
            .WithNone<Dead>();
    }

    public void Update(long tick)
    {
        ProcessIdleNpcs(tick);
        ProcessChasingNpcs(tick);
        ProcessReturningNpcs(tick);
    }

    private void ProcessIdleNpcs(long tick)
    {
        _world.Query(in _idleQuery, (Entity entity, ref NpcAI ai, ref NpcBehavior behavior, ref GridPosition position) =>
        {
            if (ai.State != NpcAIState.Idle) return;
            if (tick < ai.NextActionTick) return;

            // Wander aleatório
            if (behavior.WanderRadius > 0 && behavior.Type != NpcBehaviorType.Stationary)
            {
                ai.State = NpcAIState.Wandering;
                ai.StateChangeTick = tick;
                ai.NextActionTick = tick + _random.Next(200, 500);
            }
        });
    }

    private void ProcessChasingNpcs(long tick)
    {
        _world.Query(in _chasingQuery, (Entity entity, ref NpcAI ai, ref NpcBehavior behavior, ref GridPosition position, ref AggroTable aggro) =>
        {
            if (ai.State != NpcAIState.Chasing) return;
            if (ai.TargetEntityId == 0)
            {
                ai.State = NpcAIState.Returning;
                ai.StateChangeTick = tick;
            }
        });
    }

    private void ProcessReturningNpcs(long tick)
    {
        _world.Query(in _returningQuery, (Entity entity, ref NpcAI ai, ref SpawnInfo spawn, ref GridPosition position) =>
        {
            if (ai.State != NpcAIState.Returning) return;

            // Verifica se chegou ao spawn
            if (position.X == spawn.SpawnX && position.Y == spawn.SpawnY)
            {
                ai.State = NpcAIState.Idle;
                ai.StateChangeTick = tick;
                ai.NextActionTick = tick + 100;
            }
        });
    }

    public void Dispose() { }
}

/// <summary>
/// Sistema de respawn de NPCs.
/// </summary>
public sealed class NpcRespawnSystem(World world) : IDisposable
{
    private readonly QueryDescription _deadNpcQuery = new QueryDescription()
        .WithAll<Dead, SpawnInfo, Health, NpcAI, GridPosition, Identity>();
    private readonly List<Entity> _toRespawn = new();

    public void Update(long tick)
    {
        _toRespawn.Clear();

        // Coleta entidades para respawn
        world.Query(in _deadNpcQuery, (Entity entity, ref Identity identity, ref SpawnInfo spawn) =>
        {
            if (identity.Type == EntityType.Npc && spawn.ShouldRespawn(tick))
            {
                _toRespawn.Add(entity);
            }
        });

        // Processa respawn fora da query
        foreach (var entity in _toRespawn)
        {
            ref var health = ref world.Get<Health>(entity);
            ref var position = ref world.Get<GridPosition>(entity);
            ref var ai = ref world.Get<NpcAI>(entity);
            ref var spawn = ref world.Get<SpawnInfo>(entity);

            health.Reset();
            position.X = spawn.SpawnX;
            position.Y = spawn.SpawnY;
            ai.State = NpcAIState.Idle;
            ai.TargetEntityId = 0;
            spawn.DeathTick = 0;

            world.Remove<Dead>(entity);
        }
    }

    public void Dispose() { }
}