using Game.Domain.Enums;
using Game.Domain.Extensions;
using Game.Domain.ValueObjects.Character;

namespace Game.Domain.ValueObjects.Attributes;

/// <summary>
/// Stats de combate da entidade.
/// Component ECS para representar todos os atributos derivados de combate.
/// </summary>
public readonly record struct CombatStats(
    double PhysicalAttack,
    double MagicAttack,
    double PhysicalDefense,
    double MagicDefense,
    int AttackRange,
    double AttackSpeed,
    double CriticalChance,
    double CriticalDamage,
    double ManaCostPerAttack,
    DamageType DamageType)
{
    public static CombatStats Zero => default;
    
    public double GetTotalAttack(DamageType damageType)
    {
        double totalAttack = 0;
        if (damageType.HasFlag(DamageType.Physical))
        {
            totalAttack += PhysicalAttack;
        }
        if (damageType.HasFlag(DamageType.Magical))
        {
            totalAttack += MagicAttack;
        }
        return totalAttack;
    }
    
    public double GetTotalDefense(DamageType damageType)
    {
        double totalDefense = 0;
        if (damageType.HasFlag(DamageType.Physical))
        {
            totalDefense += PhysicalDefense;
        }
        if (damageType.HasFlag(DamageType.Magical))
        {
            totalDefense += MagicDefense;
        }
        return totalDefense;
    }

    public static CombatStats BuildFrom(BaseStats stats, Vocation vocation)
    {
        var baseStats = BuildFrom(vocation.Type);
        var modifiers = vocation.Type.GetGrowthModifiers();
        var level = vocation.Level;

        return new CombatStats(
            PhysicalAttack: baseStats.PhysicalAttack + stats.Strength * 2 + level * modifiers.Strength,
            MagicAttack: baseStats.MagicAttack + stats.Intelligence * 2 + level * modifiers.Intelligence,
            PhysicalDefense: baseStats.PhysicalDefense + stats.Constitution + level * modifiers.Constitution,
            MagicDefense: baseStats.MagicDefense + stats.Spirit + level * modifiers.Spirit,
            AttackRange: baseStats.AttackRange,
            AttackSpeed: baseStats.AttackSpeed + (stats.Dexterity / 100.0) + (level * modifiers.Dexterity / 100.0),
            CriticalChance: baseStats.CriticalChance + (stats.Dexterity / 100.0) + (level * modifiers.Dexterity / 100.0),
            CriticalDamage: baseStats.CriticalDamage,
            ManaCostPerAttack: baseStats.ManaCostPerAttack,
            DamageType: baseStats.DamageType);
    }

    private static CombatStats BuildFrom(VocationType vocation)
    {
        return vocation switch
        {
            VocationType.Warrior => BuildFrom(VocationArchetype.Melee),
            VocationType.Archer => BuildFrom(VocationArchetype.Ranged),
            VocationType.Mage => BuildFrom(VocationArchetype.Magic),
            VocationType.Cleric => BuildFrom(VocationArchetype.Hybrid),
            _ => Zero
        };
    }
    
    private static CombatStats BuildFrom(VocationArchetype archetype)
    {
        return archetype switch
        {
            VocationArchetype.Melee => new CombatStats(10, 2, 5, 3, 1, 1.0f, 5f, 150f, 0, DamageType.Physical),
            VocationArchetype.Ranged => new CombatStats(8, 3, 4, 4, 8, 1.2f, 10f, 175f, 0, DamageType.Physical),
            VocationArchetype.Magic => new CombatStats(2, 10, 3, 5,6, 0.8f, 8f, 150f, 10, DamageType.Magical),
            VocationArchetype.Hybrid => new CombatStats(6, 6, 4, 4, 1, 1.0f, 6f, 150f, 5, DamageType.Physical | DamageType.Magical),
            _ => Zero
        };
    }
    
    public static CombatStats operator +(CombatStats a, CombatStats b) => new(
        a.PhysicalAttack + b.PhysicalAttack,
        a.MagicAttack + b.MagicAttack,
        a.PhysicalDefense + b.PhysicalDefense,
        a.MagicDefense + b.MagicDefense,
        a.AttackRange + b.AttackRange,
        a.AttackSpeed + b.AttackSpeed,
        a.CriticalChance + b.CriticalChance,
        a.CriticalDamage + b.CriticalDamage,
        a.ManaCostPerAttack + b.ManaCostPerAttack,
        a.DamageType | b.DamageType);
    
    public static CombatStats operator *(CombatStats combatStats, double factor) => combatStats with
    {
        PhysicalAttack = combatStats.PhysicalAttack * factor, 
        MagicAttack = combatStats.MagicAttack * factor, 
        PhysicalDefense = combatStats.PhysicalDefense * factor, 
        MagicDefense = combatStats.MagicDefense * factor
    };
    
    public static CombatStats operator *(CombatStats combatStats, CombatStats factor) => combatStats with
    {
        PhysicalAttack = combatStats.PhysicalAttack * factor.PhysicalAttack,
        MagicAttack = combatStats.MagicAttack * factor.MagicAttack,
        PhysicalDefense = combatStats.PhysicalDefense * factor.PhysicalDefense,
        MagicDefense = combatStats.MagicDefense * factor.MagicDefense,
        DamageType = combatStats.DamageType | factor.DamageType
    };
    
}