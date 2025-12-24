using Game.Domain.Attributes.Vocation.ValueObjects;
using Game.Domain.Commons.Enums;

namespace Game.Domain.Attributes.Stats.ValueObjects;

/// <summary>
/// Stats de combate da entidade.
/// </summary>
public readonly record struct Stats(
    int BaseHealth,
    int BaseMana,
    int PhysicalDamage,
    int MagicDamage,
    int PhysicalDefense,
    int MagicDefense,
    int ManaCostPerAttack,
    float CriticalChance,
    float AttackRange,
    float AttackSpeed,
    float MovementSpeed)
{
    public static Stats FromVocation(VocationType vocation)
    {
        var stats = VocationStats.GetForVocation(vocation);
        return new Stats
        {
            BaseHealth = stats.BaseHealth,
            BaseMana = stats.BaseMana,
            PhysicalDamage = stats.BasePhysicalDamage,
            MagicDamage = stats.BaseMagicDamage,
            PhysicalDefense = stats.BasePhysicalDefense,
            MagicDefense = stats.BaseMagicDefense,
            CriticalChance = stats.BaseCriticalChance,
            AttackRange = stats.BaseAttackRange,
            AttackSpeed = stats.BaseAttackSpeed,
            MovementSpeed = stats.BaseMovementSpeed,
            ManaCostPerAttack = stats.ManaCostPerAttack
        };
    }
    
    public static Stats CalculateDerived(
        BaseStats @base, 
        Progress.ValueObjects.Progress progress, 
        StatsModifier modifiers,
        VocationStats vocationStats)
    {
        // Chance crítica baseada em Dexterity
        float criticalChance = vocationStats.BaseCriticalChance + (@base.Dexterity * 0.002f);
        criticalChance = Math.Clamp(criticalChance, 0f, 1f); // Limita entre 0% e 100%
    
        // Velocidade de ataque baseada em Dexterity
        float attackSpeed = vocationStats.BaseAttackSpeed + (@base.Dexterity * 0.01f);
    
        return new(
            PhysicalDamage: (int)modifiers.ApplyStrength(2 * @base.Strength + progress.Level),
            MagicDamage: (int)modifiers.ApplyIntelligence(3 * @base.Intelligence + @base.Spirit / 2),
            PhysicalDefense: (int)modifiers.ApplyConstitution(@base.Constitution + @base.Strength / 2),
            MagicDefense: (int)modifiers.ApplySpirit(@base.Spirit + @base.Intelligence / 2),
            ManaCostPerAttack: vocationStats.ManaCostPerAttack,
            CriticalChance: modifiers.ApplyCritical(criticalChance),
            AttackRange: vocationStats.BaseAttackRange,
            AttackSpeed: modifiers.ApplyAttackSpeed(attackSpeed),
            MovementSpeed: vocationStats.BaseMovementSpeed + (@base.Dexterity * 0.005f)
        );
    }
    
}