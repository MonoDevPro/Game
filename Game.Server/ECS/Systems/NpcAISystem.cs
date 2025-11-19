using System;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema simples de IA para NPCs melee que perseguem o jogador mais próximo e atacam ao alcance.
/// </summary>
public sealed partial class NpcAISystem(World world, IMapService mapService, ILogger<NpcAISystem>? logger = null)
    : GameSystem(world)
{
    private readonly ILogger<NpcAISystem>? _logger = logger;

    private const int AggroRange = 8;
    private const int SpatialBufferSize = 64;
    private const float AttackAnimationDuration = 0.75f;

    [Query]
    [All<AIControlled, MapId, Position, Facing, Velocity, Walkable, Attackable, CombatState, DirtyFlags>]
    [None<Dead>]
    private void ProcessNpcBehavior(
        in Entity entity,
        in MapId mapId,
        in Position position,
        ref Facing facing,
        ref Velocity velocity,
        in Walkable walkable,
        in Attackable attackable,
        ref CombatState combat,
    ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        combat.ReduceCooldown(deltaTime);

        if (!mapService.HasMap(mapId.Value))
        {
            StopMovement(ref velocity, ref dirty);
            return;
        }

        var target = AcquireTarget(mapId.Value, position);
        if (target == Entity.Null)
        {
            StopMovement(ref velocity, ref dirty);
            return;
        }

        if (!World.IsAlive(target) || World.Has<Dead>(target))
        {
            StopMovement(ref velocity, ref dirty);
            return;
        }

        ref readonly var targetPosition = ref World.Get<Position>(target);
        int distance = position.ManhattanDistance(targetPosition);

        if (distance <= SimulationConfig.MaxMeleeAttackRange)
        {
            StopMovement(ref velocity, ref dirty);
            UpdateFacingTowards(position, targetPosition, ref facing, ref dirty);
            TryPerformAttack(entity, target, in attackable, ref combat, ref dirty);
            return;
        }

        var direction = ComputeDirection(position, targetPosition);
        if (direction == default)
        {
            StopMovement(ref velocity, ref dirty);
            return;
        }

        UpdateFacing(direction, ref facing, ref dirty);
        UpdateVelocity(direction, walkable, ref velocity, ref dirty);
    }

    private Entity AcquireTarget(int mapId, in Position position)
    {
        var spatial = mapService.GetMapSpatial(mapId);
        Span<Entity> buffer = stackalloc Entity[SpatialBufferSize];
        var area = new AreaPosition(
            position.X - AggroRange,
            position.Y - AggroRange,
            position.Z,
            position.X + AggroRange,
            position.Y + AggroRange,
            position.Z);

        int count = spatial.QueryArea(area, buffer);
        Entity best = Entity.Null;
        int bestDistance = int.MaxValue;

        for (int i = 0; i < count; i++)
        {
            var candidate = buffer[i];
            if (!World.Has<PlayerControlled>(candidate) || World.Has<Dead>(candidate))
                continue;

            ref readonly var candidatePos = ref World.Get<Position>(candidate);
            int distance = position.ManhattanDistance(candidatePos);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = candidate;
            }
        }

        return best;
    }

    private void UpdateFacingTowards(in Position origin, in Position target, ref Facing facing, ref DirtyFlags dirty)
    {
        var direction = ComputeDirection(origin, target);
        if (direction == default)
            return;

        UpdateFacing(direction, ref facing, ref dirty);
    }

    private static (int X, int Y) ComputeDirection(in Position origin, in Position target)
    {
        int dx = Math.Sign(target.X - origin.X);
        int dy = Math.Sign(target.Y - origin.Y);
        return (dx, dy);
    }

    private void UpdateFacing((int X, int Y) direction, ref Facing facing, ref DirtyFlags dirty)
    {
        if (direction == default)
            return;

        if (facing.DirectionX == direction.X && facing.DirectionY == direction.Y)
            return;

        facing.DirectionX = direction.X;
        facing.DirectionY = direction.Y;
        dirty.MarkDirty(DirtyComponentType.State);
    }

    private void UpdateVelocity((int X, int Y) direction, in Walkable walkable, ref Velocity velocity, ref DirtyFlags dirty)
    {
        if (direction == default)
        {
            StopMovement(ref velocity, ref dirty);
            return;
        }

        if (velocity.DirectionX != direction.X || velocity.DirectionY != direction.Y || velocity.Speed <= 0f)
        {
            velocity.DirectionX = direction.X;
            velocity.DirectionY = direction.Y;
            velocity.Speed = walkable.BaseSpeed * walkable.CurrentModifier;
            dirty.MarkDirty(DirtyComponentType.State);
        }
    }

    private void StopMovement(ref Velocity velocity, ref DirtyFlags dirty)
    {
        if (velocity.Speed <= 0f && velocity.DirectionX == 0 && velocity.DirectionY == 0)
            return;

        velocity.Stop();
        dirty.MarkDirty(DirtyComponentType.State);
    }

    private void TryPerformAttack(
        in Entity attacker,
        in Entity target,
        in Attackable attackable,
        ref CombatState combat,
        ref DirtyFlags dirty)
    {
        if (!CombatLogic.CheckAttackCooldown(in combat))
            return;

        if (!World.TryAttack(attacker, target, AttackType.Basic, out int damage))
            return;

        combat.LastAttackTime = attackable.CalculateAttackCooldownSeconds();
        combat.InCombat = true;
        combat.TimeSinceLastHit = 0f;
        dirty.MarkDirty(DirtyComponentType.Combat);

        var attackAction = new Attack
        {
            Type = AttackType.Basic,
            RemainingDuration = AttackAnimationDuration,
            TotalDuration = AttackAnimationDuration,
            DamageApplied = false
        };
        if (World.Has<Attack>(attacker))
            World.Set(attacker, attackAction);
        else
            World.Add(attacker, attackAction);

        World.ApplyDeferredDamage(attacker, target, damage);
    }
}
