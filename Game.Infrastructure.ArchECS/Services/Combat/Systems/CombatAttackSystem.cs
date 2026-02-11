using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.Combat.Events;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;

namespace Game.Infrastructure.ArchECS.Services.Combat.Systems;

/// <summary>
/// Processa requisições de ataque básico.
/// </summary>
public sealed partial class CombatAttackSystem(
    World world,
    WorldMap map,
    CombatConfig config)
    : GameSystem(world)
{
    [Query]
    [All<AttackRequest, CombatStats, VocationTag, AttackCooldown, Position, FloorId>]
    private void ProcessAttackRequests(
        [Data] in long serverTick,
        in Entity entity,
        ref AttackRequest request,
        ref CombatStats stats,
        ref VocationTag vocation,
        ref AttackCooldown cooldown,
        ref Position position,
        ref FloorId floor)
    {
        World.Remove<AttackRequest>(entity);

        if (request.DirX == 0 && request.DirY == 0)
            return;

        if (!config.TryGetVocation(vocation.Value, out var vocationConfig))
            return;

        if (serverTick < cooldown.NextAttackTick)
            return;

        if (vocationConfig.ManaCost > 0 && stats.CurrentMana < vocationConfig.ManaCost)
            return;

        var effectiveMs = CalculateEffectiveCooldownMs(vocationConfig.BaseCooldownMs, stats.Agility);
        var cooldownTicks =
            Math.Max(1, (int)Math.Ceiling(effectiveMs / (double)SimulationConfig.TickDeltaMilliseconds));
        cooldown.CooldownTicks = cooldownTicks;
        cooldown.NextAttackTick = serverTick + cooldownTicks;

        if (vocationConfig.ManaCost > 0)
        {
            stats.CurrentMana -= vocationConfig.ManaCost;
        }

        AttackStartedEvent attackEvent = new(
            entity,
            request.DirX,
            request.DirY,
            position.X,
            position.Y,
            floor.Value);

        EventBus.Send(ref attackEvent);

        if (vocationConfig.UsesProjectile)
        {
            SpawnProjectile(entity, ref stats, ref position, ref floor, ref request, vocationConfig);
            return;
        }

        ResolveMeleeAttack(entity, ref stats, ref position, ref floor, ref request, vocationConfig);
    }

    private double CalculateEffectiveCooldownMs(int baseMs, int agility)
    {
        var reduced = baseMs - (agility * config.Cooldown.MsPerAgility);
        var minMs = baseMs * config.Cooldown.MinCooldownFactor;
        var effectiveCooldownMs = Math.Clamp(reduced, minMs, baseMs);
        return effectiveCooldownMs;
    }

    private void ResolveMeleeAttack(
        Entity attackerEntity,
        ref CombatStats attackerStats,
        ref Position position,
        ref FloorId floor,
        ref AttackRequest request,
        CombatConfig.VocationConfig vocationConfig)
    {
        var range = Math.Max(1, vocationConfig.Range);
        var dirX = Math.Clamp(request.DirX, -1, 1);
        var dirY = Math.Clamp(request.DirY, -1, 1);

        for (var step = 1; step <= range; step++)
        {
            var targetPos = new Position { X = position.X + (dirX * step), Y = position.Y + (dirY * step) };
            if (!map.InBounds(targetPos, floor.Value))
                return;

            if (!map.TryGetFirstEntity(targetPos, floor.Value, out var targetEntity) || targetEntity == Entity.Null)
                continue;

            if (targetEntity == attackerEntity)
                continue;

            TryApplyDamage(attackerEntity, targetEntity, ref attackerStats, vocationConfig, dirX, dirY);
            return;
        }
    }

    private void SpawnProjectile(
        in Entity attacker,
        ref CombatStats attackerStats,
        ref Position position,
        ref FloorId floor,
        ref AttackRequest request,
        CombatConfig.VocationConfig vocationConfig)
    {
        var dirX = Math.Clamp(request.DirX, -1, 1);
        var dirY = Math.Clamp(request.DirY, -1, 1);
        if (dirX == 0 && dirY == 0)
            return;

        var damage = CalculateDamage(in attackerStats, vocationConfig);
        var ownerTeamId = 0;
        if (World.Has<TeamId>(attacker))
            ownerTeamId = World.Get<TeamId>(attacker).Value;

        ProjectileSpawnedEvent projectileEvent = new(
            attacker,
            dirX,
            dirY,
            position.X,
            position.Y,
            floor.Value,
            vocationConfig.ProjectileSpeed,
            vocationConfig.Range,
            ownerTeamId,
            damage);

        EventBus.Send(ref projectileEvent);
    }

    private void TryApplyDamage(
        Entity attackerEntity,
        Entity targetEntity,
        ref CombatStats attackerStats,
        CombatConfig.VocationConfig vocationConfig,
        int dirX,
        int dirY)
    {
        if (!World.Has<CombatStats>(targetEntity))
            return;

        if (World.Has<CharacterId>(targetEntity) &&
            (map.Flags & MapFlags.PvPEnabled) == 0)
            return;

        var attackerTeam = World.Has<TeamId>(attackerEntity) ? World.Get<TeamId>(attackerEntity).Value : 0;
        var targetTeam = World.Has<TeamId>(targetEntity) ? World.Get<TeamId>(targetEntity).Value : 0;
        if (attackerTeam != 0 && attackerTeam == targetTeam)
            return;

        ref var targetStats = ref World.Get<CombatStats>(targetEntity);
        if (targetStats.CurrentHealth <= 0)
            return;

        var damage = CalculateDamage(in attackerStats, vocationConfig);
        if (damage <= 0)
            return;

        targetStats.CurrentHealth = Math.Max(0, targetStats.CurrentHealth - damage);

        var targetPos = World.Has<Position>(targetEntity) ? World.Get<Position>(targetEntity) : new Position();
        var targetFloor = World.Has<FloorId>(targetEntity) ? World.Get<FloorId>(targetEntity).Value : 0;


        CombatDamageEvent damageEvent = new(
            attackerEntity,
            targetEntity,
            damage,
            dirX,
            dirY,
            targetPos.X,
            targetPos.Y,
            targetFloor);

        EventBus.Send(ref damageEvent);
    }

    private int CalculateDamage(in CombatStats stats, CombatConfig.VocationConfig configData)
    {
        var stat = configData.DamageStat switch
        {
            CombatDamageStat.Strength => stats.Strength,
            CombatDamageStat.Endurance => stats.Endurance,
            CombatDamageStat.Agility => stats.Agility,
            CombatDamageStat.Intelligence => stats.Intelligence,
            CombatDamageStat.Willpower => stats.Willpower,
            _ => stats.Strength
        };

        var scaled = stat * configData.DamageScale;
        var damage = configData.DamageBase + (int)MathF.Round(scaled);
        return Math.Max(0, damage);
    }
}