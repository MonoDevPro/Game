using Game.Domain.Commons;
using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Attributes;
using Game.Domain.Vocations;
using Game.Domain.Vocations.ValueObjects;

namespace Game.Domain.Combat.ValueObjects;

/// <summary>
/// Stats de combate da entidade.
/// Component ECS para representar todos os atributos derivados de combate.
/// 
/// Convenções de escala:
/// - AttackSpeedPermille: 1000 = 1.000x
/// - CriticalChanceBps:  10_000 = 100.00%
/// - CriticalDamagePermille: 1000 = 1.000x
/// </summary>
public readonly record struct CombatStats(
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense,
    int AttackRange,
    int AttackSpeedPermille,
    int CriticalChanceBps,
    int CriticalDamagePermille,
    int ManaCostPerAttack,
    DamageType DamageType)
{
    public static CombatStats Zero => default;

    public int GetTotalAttack(DamageType damageType)
    {
        int totalAttack = 0;
        if (damageType.HasFlag(DamageType.Physical))
            totalAttack += PhysicalAttack;

        if (damageType.HasFlag(DamageType.Magical))
            totalAttack += MagicAttack;

        return totalAttack;
    }

    public int GetTotalDefense(DamageType damageType)
    {
        int totalDefense = 0;
        if (damageType.HasFlag(DamageType.Physical))
            totalDefense += PhysicalDefense;

        if (damageType.HasFlag(DamageType.Magical))
            totalDefense += MagicDefense;

        return totalDefense;
    }

    /// <summary>
    /// Constrói CombatStats a partir dos stats do jogador (já incluindo bônus) + vocação + level.
    /// GrowthModifiers usam escala GameConstants.Scaling.GROWTH_SCALE (10 => 1.0).
    /// </summary>
    public static CombatStats BuildFrom(BaseStats stats, Vocation vocation, int level)
    {
        var baseCombat = BuildBaseByVocation(vocation.Type);
        var growth = vocation.Type.GetGrowthModifiers();

        // Crescimento de atributos base por level (em pontos de atributo)
        int str = stats.Strength + (level * growth.Strength) / GameConstants.Scaling.GROWTH_SCALE;
        int dex = stats.Dexterity + (level * growth.Dexterity) / GameConstants.Scaling.GROWTH_SCALE;
        int intel = stats.Intelligence + (level * growth.Intelligence) / GameConstants.Scaling.GROWTH_SCALE;
        int con = stats.Constitution + (level * growth.Constitution) / GameConstants.Scaling.GROWTH_SCALE;
        int spr = stats.Spirit + (level * growth.Spirit) / GameConstants.Scaling.GROWTH_SCALE;

        int physicalAttack = baseCombat.PhysicalAttack + (str * GameConstants.Combat.CombatStats.PHYSICAL_ATTACK_PER_STR);
        int magicAttack = baseCombat.MagicAttack + (intel * GameConstants.Combat.CombatStats.MAGIC_ATTACK_PER_INT);
        int physicalDefense = baseCombat.PhysicalDefense + (con * GameConstants.Combat.CombatStats.PHYSICAL_DEFENSE_PER_CON);
        int magicDefense = baseCombat.MagicDefense + (spr * GameConstants.Combat.CombatStats.MAGIC_DEFENSE_PER_SPR);

        int attackSpeed = baseCombat.AttackSpeedPermille + (dex * GameConstants.Combat.CombatStats.ATTACK_SPEED_PERMILLE_PER_DEX);
        attackSpeed = Math.Clamp(attackSpeed, GameConstants.Combat.MIN_ATTACK_SPEED, GameConstants.Combat.MAX_ATTACK_SPEED);

        int critChance = baseCombat.CriticalChanceBps + (dex * GameConstants.Combat.CombatStats.CRIT_CHANCE_BPS_PER_DEX);
        critChance = Math.Clamp(critChance, 0, GameConstants.Combat.CRIT_CHANCE_SCALE);

        int critDamage = Math.Clamp(baseCombat.CriticalDamagePermille, GameConstants.Combat.CRIT_DAMAGE_SCALE, 5000);

        return new CombatStats(
            PhysicalAttack: physicalAttack,
            MagicAttack: magicAttack,
            PhysicalDefense: physicalDefense,
            MagicDefense: magicDefense,
            AttackRange: baseCombat.AttackRange,
            AttackSpeedPermille: attackSpeed,
            CriticalChanceBps: critChance,
            CriticalDamagePermille: critDamage,
            ManaCostPerAttack: baseCombat.ManaCostPerAttack,
            DamageType: baseCombat.DamageType);
    }

    private static CombatStats BuildBaseByVocation(VocationType vocation) =>
        vocation switch
        {
            VocationType.Warrior => BuildBaseByArchetype(VocationArchetype.Melee),
            VocationType.Archer => BuildBaseByArchetype(VocationArchetype.Ranged),
            VocationType.Mage => BuildBaseByArchetype(VocationArchetype.Magic),
            VocationType.Cleric => BuildBaseByArchetype(VocationArchetype.Hybrid),
            _ => Zero
        };

    private static CombatStats BuildBaseByArchetype(VocationArchetype archetype) =>
        archetype switch
        {
            // crit chance em BPS (ex.: 5% = 500)
            // crit damage em permille (ex.: 150% = 1500)
            VocationArchetype.Melee  => new CombatStats(10, 2, 5, 3, 1, 1000,  500, 1500,  0, DamageType.Physical),
            VocationArchetype.Ranged => new CombatStats( 8, 3, 4, 4, 8, 1200, 1000, 1750,  0, DamageType.Physical),
            VocationArchetype.Magic  => new CombatStats( 2,10, 3, 5, 6,  800,  800, 1500, 10, DamageType.Magical),
            VocationArchetype.Hybrid => new CombatStats( 6, 6, 4, 4, 1, 1000,  600, 1500,  5, DamageType.Physical | DamageType.Magical),
            _ => Zero
        };

    public static CombatStats operator +(CombatStats a, CombatStats b) => new(
        a.PhysicalAttack + b.PhysicalAttack,
        a.MagicAttack + b.MagicAttack,
        a.PhysicalDefense + b.PhysicalDefense,
        a.MagicDefense + b.MagicDefense,
        a.AttackRange + b.AttackRange,
        a.AttackSpeedPermille + b.AttackSpeedPermille,
        a.CriticalChanceBps + b.CriticalChanceBps,
        a.CriticalDamagePermille + b.CriticalDamagePermille,
        a.ManaCostPerAttack + b.ManaCostPerAttack,
        a.DamageType | b.DamageType);

    public static CombatStats operator *(CombatStats combatStats, int factor) => combatStats with
    {
        PhysicalAttack = combatStats.PhysicalAttack * factor,
        MagicAttack = combatStats.MagicAttack * factor,
        PhysicalDefense = combatStats.PhysicalDefense * factor,
        MagicDefense = combatStats.MagicDefense * factor
    };
}