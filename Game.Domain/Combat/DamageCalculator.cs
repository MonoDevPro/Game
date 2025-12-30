using System.Runtime.CompilerServices;
using Game.Domain.Combat.Enums;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Commons;
using Game.Domain.Commons.Enums;

namespace Game.Domain.Combat;

/// <summary>
/// Sistema de cálculo de dano e combate (inteiros com escala).
/// 
/// Nota: este arquivo usa Random.Shared apenas para exemplo.
/// Para determinismo/rollback: injete o roll (0..9999) via parâmetro.
/// </summary>
public static class DamageCalculator
{
    /// <summary>
    /// Retorna true se o ataque for crítico. Chance em BPS (10_000 = 100%).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RollCriticalBps(int criticalChanceBps, int? rollBps = null)
    {
        criticalChanceBps = Math.Clamp(criticalChanceBps, 0, GameConstants.Combat.CRIT_CHANCE_SCALE);

        int roll = rollBps ?? Random.Shared.Next(0, GameConstants.Combat.CRIT_CHANCE_SCALE);
        return roll < criticalChanceBps;
    }

    /// <summary>
    /// Aplica multiplicador (permille) em um valor inteiro.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ApplyMultiplierPermille(int value, int multiplierPermille)
    {
        return (value * multiplierPermille) / GameConstants.Scaling.MULTIPLIER_PERMILLE;
    }

    /// <summary>
    /// Mitigação por defesa (diminishing returns).
    /// Fórmula inteira: final = max(1, raw * 100 / (100 + defense))
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ApplyDefenseMitigation(int rawDamage, int defense)
    {
        if (rawDamage <= 0) return 0;
        if (defense <= 0) return rawDamage;

        int denom = 100 + defense;
        int reduced = (rawDamage * 100) / denom;
        return Math.Max(1, reduced);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateDistance(int x1, int y1, int x2, int y2) =>
        Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1)); // Chebyshev

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRange(int attackerX, int attackerY, int targetX, int targetY, int range) =>
        CalculateDistance(attackerX, attackerY, targetX, targetY) <= range;

    /// <summary>
    /// Cooldown em ticks a partir do AttackSpeed (permille).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateAttackCooldownTicks(int baseCooldownTicks, int attackSpeedPermille) =>
        CombatCalculator.ComputeAttackCooldownTicks(baseCooldownTicks, attackSpeedPermille);

    /// <summary>
    /// Cálculo completo de dano de ataque básico.
    /// - rawDamage e finalDamage retornam em int, mas DamageMessage usa double (mantido por compatibilidade).
    /// - critRollBps: se fornecido, garante determinismo (0..9999).
    /// </summary>
    public static DamageMessage CalculateFullDamage(
        in CombatStats attackerStats,
        in CombatStats targetStats,
        int attackerId,
        int targetId,
        int? critRollBps = null)
    {
        int rawDamage = attackerStats.GetTotalAttack(attackerStats.DamageType);

        bool isCritical = RollCriticalBps(attackerStats.CriticalChanceBps, critRollBps);
        if (isCritical)
        {
            rawDamage = ApplyMultiplierPermille(rawDamage, attackerStats.CriticalDamagePermille);
        }

        int defense = attackerStats.DamageType == DamageType.True
            ? 0
            : targetStats.GetTotalDefense(attackerStats.DamageType);

        int finalDamage = attackerStats.DamageType == DamageType.True
            ? Math.Max(1, rawDamage)
            : ApplyDefenseMitigation(rawDamage, defense);

        return DamageMessage.Create(
            attackerId,
            targetId,
            attackerStats.DamageType,
            rawDamage,
            finalDamage,
            isCritical,
            AttackResult.Hit);
    }

    /// <summary>
    /// Compat: assinatura antiga (double critMultiplier). Converte para permille quando possível.
    /// </summary>
    public static DamageMessage CalculateFullDamage(
        in CombatStats attackerStats,
        in CombatStats targetStats,
        double critMultiplier,
        int attackerId,
        int targetId)
    {
        // Converte 1.5 => 1500 (permille). Se vier 150, assume percent => 1500.
        int permille = critMultiplier switch
        {
            <= 0 => GameConstants.Combat.DEFAULT_CRIT_DAMAGE,
            < 10 => (int)Math.Round(critMultiplier * GameConstants.Scaling.MULTIPLIER_PERMILLE),
            _ => (int)Math.Round(critMultiplier * 10) // 150 => 1500
        };

        var patched = attackerStats with { CriticalDamagePermille = permille };
        return CalculateFullDamage(patched, targetStats, attackerId, targetId, null);
    }
}
