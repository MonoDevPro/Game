using System.Runtime.CompilerServices;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Combat.Shared.Data;

namespace GameECS.Modules.Combat.Shared.Core;

/// <summary>
/// Sistema de cálculo de dano e combate.
/// Thread-safe para cálculos puros.
/// </summary>
public static class DamageCalculator
{
    private static readonly Random _random = new();
    
    /// <summary>
    /// Calcula o dano base para um ataque.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateBaseDamage(
        in CombatStats attackerStats,
        VocationType attackerVocation)
    {
        return attackerVocation switch
        {
            VocationType.Mage => attackerStats.MagicDamage,
            VocationType.Archer => attackerStats.PhysicalDamage,
            VocationType.Knight => attackerStats.PhysicalDamage,
            _ => attackerStats.PhysicalDamage
        };
    }

    /// <summary>
    /// Calcula a defesa efetiva contra um tipo de dano.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int CalculateDefense(
        in CombatStats targetStats,
        DamageType damageType)
    {
        return damageType switch
        {
            DamageType.Physical => targetStats.PhysicalDefense,
            DamageType.Magic => targetStats.MagicDefense,
            DamageType.True => 0,  // True damage ignora defesas
            _ => 0
        };
    }

    /// <summary>
    /// Aplica mitigação de dano baseado em defesa.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ApplyDefenseMitigation(int rawDamage, int defense)
    {
        // Fórmula: Dano Final = Dano * (100 / (100 + Defesa))
        // Isso dá diminishing returns para defesa alta
        float mitigation = 100f / (100f + defense);
        return Math.Max(1, (int)(rawDamage * mitigation));
    }

    /// <summary>
    /// Verifica se o ataque é crítico.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool RollCritical(float criticalChance)
    {
        return _random.NextSingle() * 100f < criticalChance;
    }

    /// <summary>
    /// Aplica multiplicador de crítico.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ApplyCriticalDamage(int damage, float critMultiplier)
    {
        return (int)(damage * critMultiplier);
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
    public static int CalculateAttackCooldown(int baseCooldownTicks, float attackSpeed)
    {
        // Maior attack speed = menor cooldown
        return Math.Max(1, (int)(baseCooldownTicks / attackSpeed));
    }

    /// <summary>
    /// Executa cálculo completo de dano de ataque básico.
    /// </summary>
    public static DamageInfo CalculateFullDamage(
        in CombatStats attackerStats,
        in CombatStats targetStats,
        VocationType attackerVocation,
        float critMultiplier,
        int attackerId,
        int targetId,
        long tick)
    {
        // 1. Calcula dano base
        int rawDamage = CalculateBaseDamage(attackerStats, attackerVocation);
        
        // 2. Determina tipo de dano
        DamageType damageType = attackerVocation == VocationType.Mage 
            ? DamageType.Magic 
            : DamageType.Physical;

        // 3. Verifica crítico
        bool isCritical = RollCritical(attackerStats.CriticalChance);
        if (isCritical)
        {
            rawDamage = ApplyCriticalDamage(rawDamage, critMultiplier);
        }

        // 4. Aplica defesa
        int defense = CalculateDefense(targetStats, damageType);
        int finalDamage = ApplyDefenseMitigation(rawDamage, defense);

        return DamageInfo.Create(
            rawDamage,
            finalDamage,
            damageType,
            isCritical,
            attackerId,
            targetId,
            tick);
    }

    /// <summary>
    /// Calcula dano específico por vocação com modificadores.
    /// </summary>
    public static int CalculateVocationDamage(
        VocationType vocation,
        in CombatStats stats,
        int targetDefense,
        DamageType damageType)
    {
        int baseDamage = vocation switch
        {
            // Knight: Dano físico direto
            VocationType.Knight => stats.PhysicalDamage,
            
            // Mage: Dano mágico com bônus de 10%
            VocationType.Mage => (int)(stats.MagicDamage * 1.1f),
            
            // Archer: Dano físico com bônus de velocidade
            VocationType.Archer => (int)(stats.PhysicalDamage * stats.AttackSpeed),
            
            _ => stats.PhysicalDamage
        };

        return ApplyDefenseMitigation(baseDamage, targetDefense);
    }
}
