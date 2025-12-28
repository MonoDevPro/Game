using System.Runtime.CompilerServices;
using Game.Domain.Combat.Enums;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Enums;
using Game.Domain.ValueObjects.Attributes;

namespace Game.Domain.Combat.Core;

/// <summary>
/// Sistema de cálculo de dano e combate.
/// Thread-safe para cálculos puros.
/// </summary>
public static class DamageCalculator
{
    private static readonly Random Random = new();
    
    /// <summary>
    /// Calcula o dano base para um ataque.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateBaseDamage(
        in CombatStats attackerStats,
        VocationType attackerVocation)
    {
        return attackerVocation switch
        {
            VocationType.Mage => attackerStats.MagicAttack,
            _ => attackerStats.PhysicalAttack
        };
    }

    /// <summary>
    /// Calcula a defesa efetiva contra um tipo de dano.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double CalculateDefense(
        in CombatStats targetStats,
        DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => targetStats.PhysicalDefense,
            DamageType.Magical => targetStats.MagicDefense,
            DamageType.True => 0,  // True damage ignora defesas
            _ => 0
        };
    }

    /// <summary>
    /// Aplica mitigação de dano baseado em defesa.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ApplyDefenseMitigation(double rawDamage, double defense)
    {
        // Fórmula: Dano Final = Dano * (100 / (100 + Defesa))
        // Isso dá diminishing returns para defesa alta
        double mitigation = 100f / (100f + defense);
        return Math.Max(1, rawDamage * mitigation);
    }

    /// <summary>
    /// Verifica se o ataque é crítico.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RollCritical(double criticalChance)
    {
        return Random.NextSingle() * 100f < criticalChance;
    }

    /// <summary>
    /// Aplica multiplicador de crítico.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static double ApplyCriticalDamage(double damage, double critMultiplier)
    {
        return damage * critMultiplier;
    }

    /// <summary>
    /// Calcula a distância entre duas posições de grid.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateDistance(int x1, int y1, int x2, int y2)
    {
        // Usa Chebyshev distance (permite movimento diagonal)
        return Math.Max(Math.Abs(x2 - x1), Math.Abs(y2 - y1));
    }

    /// <summary>
    /// Verifica se o alvo está dentro do alcance de ataque.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsInRange(int attackerX, int attackerY, int targetX, int targetY, int range)
    {
        return CalculateDistance(attackerX, attackerY, targetX, targetY) <= range;
    }

    /// <summary>
    /// Calcula o cooldown efetivo de ataque em ticks.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateAttackCooldown(int baseCooldownTicks, double attackSpeed)
    {
        // Maior attack speed = menor cooldown
        return Math.Max(1, (int)(baseCooldownTicks / attackSpeed));
    }

    /// <summary>
    /// Executa cálculo completo de dano de ataque básico.
    /// </summary>
    public static DamageMessage CalculateFullDamage(
        in CombatStats attackerStats,
        in CombatStats targetStats,
        double critMultiplier,
        int attackerId,
        int targetId)
    {
        // 1. Calcula dano base
        double rawDamage = attackerStats.GetTotalAttack(attackerStats.DamageType);
        
        // 2. Verifica crítico
        bool isCritical = RollCritical(attackerStats.CriticalChance);
        if (isCritical)
        {
            rawDamage = ApplyCriticalDamage(rawDamage, critMultiplier);
        }

        // 4. Aplica defesa
        double defense = targetStats.GetTotalDefense(attackerStats.DamageType);
        
        double finalDamage = ApplyDefenseMitigation(rawDamage, defense);

        return DamageMessage.Create(
            attackerId,
            targetId,
            attackerStats.DamageType,
            rawDamage,
            finalDamage,
            isCritical,
            AttackResult.Hit);
    }
}
