using Arch.Core;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Entities.Shared.Components;
using GameECS.Modules.Entities.Shared.Data;
using GameECS.Modules.Navigation.Shared.Components;

namespace GameECS.Modules.Entities.Server.Systems;

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
public sealed class NpcRespawnSystem : IDisposable
{
    private readonly World _world;
    private readonly QueryDescription _deadNpcQuery;
    private readonly List<Entity> _toRespawn = new();

    public NpcRespawnSystem(World world)
    {
        _world = world;
        _deadNpcQuery = new QueryDescription()
            .WithAll<Dead, SpawnInfo, Health, NpcAI, GridPosition, EntityIdentity>();
    }

    public void Update(long tick)
    {
        _toRespawn.Clear();

        // Coleta entidades para respawn
        _world.Query(in _deadNpcQuery, (Entity entity, ref EntityIdentity identity, ref SpawnInfo spawn) =>
        {
            if (identity.Type == EntityType.Npc && spawn.ShouldRespawn(tick))
            {
                _toRespawn.Add(entity);
            }
        });

        // Processa respawn fora da query
        foreach (var entity in _toRespawn)
        {
            ref var health = ref _world.Get<Health>(entity);
            ref var position = ref _world.Get<GridPosition>(entity);
            ref var ai = ref _world.Get<NpcAI>(entity);
            ref var spawn = ref _world.Get<SpawnInfo>(entity);

            health.Reset();
            position.X = spawn.SpawnX;
            position.Y = spawn.SpawnY;
            ai.State = NpcAIState.Idle;
            ai.TargetEntityId = 0;
            spawn.DeathTick = 0;

            _world.Remove<Dead>(entity);
        }
    }

    public void Dispose() { }
}

/// <summary>
/// Sistema de aggro.
/// </summary>
public sealed class AggroSystem : IDisposable
{
    private readonly World _world;
    private readonly QueryDescription _aggroQuery;

    public AggroSystem(World world)
    {
        _world = world;
        _aggroQuery = new QueryDescription()
            .WithAll<AggroTable, NpcAI>()
            .WithNone<Dead>();
    }

    public void Update(long tick)
    {
        _world.Query(in _aggroQuery, (ref AggroTable aggro, ref NpcAI ai) =>
        {
            // Decay de 1% por tick quando fora de combate
            if (ai.State == NpcAIState.Idle || ai.State == NpcAIState.Returning)
            {
                aggro.DecayThreat(0.01f);
            }
        });
    }

    public void Dispose() { }
}
