using System;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;
using Game.ECS.Services;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável pela IA de NPCs e entidades controladas por IA.
/// Processa movimento, decisões de combate e comportamento de NPCs.
/// </summary>
public sealed partial class AISystem : GameSystem
{
    private readonly IMapService _mapService;
    private readonly CombatSystem _combatSystem;
    private readonly Random _random = new();

    public AISystem(World world, GameEventSystem events, EntityFactory factory, IMapService mapService, CombatSystem combatSystem)
        : base(world, events, factory)
    {
        _mapService = mapService;
        _combatSystem = combatSystem;
    }

    [Query]
    [All<AIControlled, AIState, Position, Velocity, Facing>]
    [None<Dead>]
    private void ProcessAI(
        in Entity e,
        ref AIState aiState,
        ref Position pos,
        ref Velocity vel,
        ref Facing facing,
        [Data] float deltaTime)
    {
        aiState.DecisionCooldown -= deltaTime;
        if (aiState.DecisionCooldown > 0f)
            return;

        aiState.DecisionCooldown = 0.5f + _random.NextSingle();

        switch (aiState.CurrentBehavior)
        {
            case AIBehavior.Idle:
                if (_random.NextSingle() < 0.3f)
                {
                    aiState.CurrentBehavior = AIBehavior.Wander;
                    ChooseRandomDirection(e, ref vel, ref facing);
                }
                else
                {
                    ClearVelocity(ref vel);
                }
                break;

            case AIBehavior.Wander:
                ChooseRandomDirection(e, ref vel, ref facing);
                break;

            case AIBehavior.Patrol:
            case AIBehavior.Chase:
            case AIBehavior.Attack:
            case AIBehavior.Flee:
                // Comportamentos reservados para iterações futuras.
                break;
        }
    }

    [Query]
    [All<AIControlled, CombatState, Position, Health, AIState, MapId, DirtyFlags>]
    [None<Dead>]
    private void ProcessAICombat(
        in Entity e,
        ref CombatState combat,
        ref AIState aiState,
        in Position pos,
        in Health health,
        in MapId mapId,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        if (health.Current <= 0)
            return;

        if (combat.LastAttackTime > 0f)
            return;

        if (!TryAcquireTarget(e, pos, mapId, out var target))
        {
            aiState.TargetNetworkId = 0;
            if (aiState.CurrentBehavior == AIBehavior.Attack)
                aiState.CurrentBehavior = AIBehavior.Wander;

            if (combat.InCombat)
            {
                combat.InCombat = false;
                combat.TargetNetworkId = 0;
                dirty.MarkDirty(DirtyComponentType.Combat);
                Events.RaiseCombatExit(e);
            }

            return;
        }

        if (_combatSystem.TryAttack(e, target))
        {
            aiState.CurrentBehavior = AIBehavior.Attack;
            if (World.TryGet(target, out NetworkId netId))
                aiState.TargetNetworkId = netId.Value;
            return;
        }

        // Ataque falhou (fora de alcance ou inválido). Ajusta comportamento para perseguir.
        aiState.CurrentBehavior = AIBehavior.Chase;
    }

    private void ChooseRandomDirection(in Entity entity, ref Velocity velocity, ref Facing facing)
    {
        int previousX = facing.DirectionX;
        int previousY = facing.DirectionY;

        int randomDir = _random.Next(0, 5);
        (velocity.DirectionX, velocity.DirectionY) = randomDir switch
        {
            0 => (1, 0),
            1 => (-1, 0),
            2 => (0, 1),
            3 => (0, -1),
            _ => (0, 0)
        };

        facing.DirectionX = velocity.DirectionX;
        facing.DirectionY = velocity.DirectionY;

        velocity.Speed = (velocity.DirectionX, velocity.DirectionY) switch
        {
            (0, 0) => 0f,
            _ => 3f
        };

        if (previousX != facing.DirectionX || previousY != facing.DirectionY)
        {
            Events.RaiseFacingChanged(entity, facing.DirectionX, facing.DirectionY);
        }
    }

    private static void ClearVelocity(ref Velocity velocity)
    {
        velocity.DirectionX = 0;
        velocity.DirectionY = 0;
        velocity.Speed = 0f;
    }

    /// <summary>
    /// Faz uma entidade IA atacar um alvo.
    /// </summary>
    public bool TryAIAttack(Entity attacker, Entity target)
    {
        return _combatSystem.TryAttack(attacker, target);
    }

    /// <summary>
    /// Para entidade IA de atacar.
    /// </summary>
    public void StopAICombat(Entity entity)
    {
        if (!World.IsAlive(entity) || !World.TryGet(entity, out CombatState combat))
            return;

        if (!combat.InCombat)
            return;

        combat.InCombat = false;
        combat.TargetNetworkId = 0;
        World.Set(entity, combat);

        if (World.Has<DirtyFlags>(entity))
        {
            ref DirtyFlags dirty = ref World.Get<DirtyFlags>(entity);
            dirty.MarkDirty(DirtyComponentType.Combat);
        }

        if (World.Has<AIState>(entity))
        {
            ref AIState aiState = ref World.Get<AIState>(entity);
            aiState.CurrentBehavior = AIBehavior.Wander;
            aiState.TargetNetworkId = 0;
        }

        Events.RaiseCombatExit(entity);
    }

    private bool TryAcquireTarget(in Entity self, in Position position, in MapId mapId, out Entity target)
    {
        var spatial = _mapService.GetMapSpatial(mapId.Value);
        int radius = SimulationConfig.MaxMeleeAttackRange;

        var min = new Position { X = position.X - radius, Y = position.Y - radius, Z = position.Z };
        var max = new Position { X = position.X + radius, Y = position.Y + radius, Z = position.Z };

        Span<Entity> nearby = stackalloc Entity[16];
        int count = spatial.QueryArea(min, max, nearby);

        for (int i = 0; i < count; i++)
        {
            var candidate = nearby[i];
            if (candidate == self)
                continue;

            if (!World.IsAlive(candidate) || World.Has<Dead>(candidate))
                continue;

            if (!World.Has<PlayerControlled>(candidate))
                continue;

            target = candidate;
            return true;
        }

        target = Entity.Null;
        return false;
    }
}