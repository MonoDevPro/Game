using Arch.Core;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Combat.Shared.Core;
using GameECS.Modules.Combat.Shared.Data;

namespace GameECS.Modules.Combat.Shared.Systems;

/// <summary>
/// Serviço de combate compartilhado para validações e cálculos.
/// </summary>
public sealed class CombatService
{
    private readonly CombatConfig _config;
    private readonly CombatLog _log;

    public CombatService(CombatConfig config, CombatLog? log = null)
    {
        _config = config;
        _log = log ?? new CombatLog();
    }

    public CombatLog Log => _log;
    public CombatConfig Config => _config;

    /// <summary>
    /// Valida se um ataque pode ser executado.
    /// </summary>
    public AttackResult ValidateAttack(
        World world,
        Entity attacker,
        Entity target,
        int attackerX, int attackerY,
        int targetX, int targetY,
        long currentTick)
    {
        // Verifica se atacante pode atacar
        if (!world.Has<CanAttack>(attacker))
            return AttackResult.OnCooldown;

        // Verifica se o alvo está morto
        if (world.Has<Dead>(target))
            return AttackResult.TargetDead;

        // Verifica invulnerabilidade
        if (world.Has<Invulnerable>(target))
            return AttackResult.Blocked;

        // Verifica cooldown
        if (world.Has<AttackCooldown>(attacker))
        {
            ref var cooldown = ref world.Get<AttackCooldown>(attacker);
            if (!cooldown.IsReady(currentTick))
                return AttackResult.OnCooldown;
        }

        // Verifica range
        ref var attackerStats = ref world.Get<CombatStats>(attacker);
        int distance = DamageCalculator.CalculateDistance(attackerX, attackerY, targetX, targetY);
        if (distance > attackerStats.AttackRange)
            return AttackResult.OutOfRange;

        // Verifica mana para Mage
        if (world.Has<Vocation>(attacker))
        {
            ref var vocation = ref world.Get<Vocation>(attacker);
            if (vocation.Type == VocationType.Mage && world.Has<Mana>(attacker))
            {
                ref var mana = ref world.Get<Mana>(attacker);
                var vocationStats = vocation.GetBaseStats();
                if (mana.Current < vocationStats.ManaCostPerAttack)
                    return AttackResult.InsufficientMana;
            }
        }

        return AttackResult.Hit;
    }

    /// <summary>
    /// Executa um ataque básico e retorna o resultado.
    /// </summary>
    public (AttackResult result, DamageInfo? damage) ExecuteBasicAttack(
        World world,
        Entity attacker,
        Entity target,
        int attackerX, int attackerY,
        int targetX, int targetY,
        long currentTick)
    {
        var validation = ValidateAttack(world, attacker, target, attackerX, attackerY, targetX, targetY, currentTick);
        if (validation != AttackResult.Hit)
            return (validation, null);

        ref var attackerStats = ref world.Get<CombatStats>(attacker);
        ref var targetStats = ref world.Get<CombatStats>(target);
        ref var attackerVocation = ref world.Get<Vocation>(attacker);

        // Calcula dano
        var damageInfo = DamageCalculator.CalculateFullDamage(
            in attackerStats,
            in targetStats,
            attackerVocation.Type,
            _config.CriticalDamageMultiplier,
            attacker.Id,
            target.Id,
            currentTick);

        // Consome mana se for Mage
        if (attackerVocation.Type == VocationType.Mage && world.Has<Mana>(attacker))
        {
            ref var mana = ref world.Get<Mana>(attacker);
            var vocationStats = attackerVocation.GetBaseStats();
            mana.TryConsume(vocationStats.ManaCostPerAttack);
        }

        // Aplica dano
        ref var targetHealth = ref world.Get<Health>(target);
        targetHealth.TakeDamage(damageInfo.FinalDamage);

        // Atualiza cooldown
        if (world.Has<AttackCooldown>(attacker))
        {
            ref var cooldown = ref world.Get<AttackCooldown>(attacker);
            int cdTicks = DamageCalculator.CalculateAttackCooldown(
                _config.BaseAttackCooldownTicks, 
                attackerStats.AttackSpeed);
            cooldown.TriggerCooldown(currentTick, cdTicks);
        }

        // Registra no log
        _log.LogDamage(damageInfo);

        // Marca como em combate
        if (!world.Has<InCombat>(attacker))
            world.Add(attacker, new InCombat { LastCombatTick = currentTick });
        else
            world.Get<InCombat>(attacker).LastCombatTick = currentTick;

        if (!world.Has<InCombat>(target))
            world.Add(target, new InCombat { LastCombatTick = currentTick });
        else
            world.Get<InCombat>(target).LastCombatTick = currentTick;

        // Verifica morte
        if (targetHealth.IsDead)
        {
            if (!world.Has<Dead>(target))
                world.Add<Dead>(target);

            _log.LogDeath(target.Id, attacker.Id, currentTick);
        }

        var result = damageInfo.IsCritical ? AttackResult.Critical : AttackResult.Hit;
        return (result, damageInfo);
    }

    /// <summary>
    /// Calcula o alcance máximo para uma vocação.
    /// </summary>
    public int GetMaxRangeForVocation(VocationType vocation)
    {
        return vocation switch
        {
            VocationType.Knight => _config.MaxMeleeRange,
            VocationType.Mage => _config.MaxMagicRange,
            VocationType.Archer => _config.MaxRangedRange,
            _ => _config.MaxMeleeRange
        };
    }
}
