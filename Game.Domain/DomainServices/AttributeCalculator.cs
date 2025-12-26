using Game.Domain.Extensions;
using Game.Domain.ValueObjects.Attributes;
using Game.Domain.ValueObjects.Vitals;
using Game.Domain.ValueObjects.Character;
using Game.Domain.ValueObjects.Combat;

namespace Game.Domain.DomainServices;

/// <summary>
/// Calculador de atributos do personagem.
/// Centraliza todas as fórmulas de cálculo do domínio.
/// </summary>
public static class AttributeCalculator
{
    private const float AttackSpeedDivisor = 100.0f;
    private const float MovementSpeedDivisor = 200.0f;
    
    public static (Health Health, Mana Mana) CalculateVitals(BaseStats total, Progress progress)
    {
        var maxHp = CombatConfig.HpPerConstitution * total.Constitution + progress.Level * CombatConfig.HpPerLevel;
        var maxMp = CombatConfig.MpPerIntelligence * total.Intelligence + progress.Level * CombatConfig.MpPerLevel;
        var hpRegenPerTick = Math.Max(CombatConfig.MinRegenPerTick, total.Constitution / CombatConfig.RegenDivisor);
        var mpRegenPerTick= Math.Max(CombatConfig.MinRegenPerTick, total.Spirit / CombatConfig.RegenDivisor);

        return new(
            new Health(
                max: maxHp, 
                regenPerTick: hpRegenPerTick),
            new Mana(
                max: maxMp, 
                regenPerTick: mpRegenPerTick));
    }

    public static CombatStats CalculateCombatStats(BaseStats stats, Progress progress, Vocation vocation)
    {
        var modifiers = vocation.Type.GetGrowthModifiers();
        
        return new CombatStats(
            PhysicalAttack: stats.Strength * 2 + progress.Level * modifiers.Strength,
            MagicAttack: stats.Intelligence * 2 + progress.Level * modifiers.Intelligence,
            PhysicalDefense: stats.Constitution + progress.Level * modifiers.Constitution,
            MagicDefense: stats.Spirit + progress.Level * modifiers.Spirit
        );
    }
    
    public static CombatProfile CalculateCombatProfile(BaseStats stats, Progress progress, Vocation vocation)
    {
        var modifiers = vocation.Type.GetGrowthModifiers();
        var vocationProfile = vocation.Type.GetCombatProfile();
        return new CombatProfile(
            AttackRange: vocationProfile.BaseAttackRange,
            AttackSpeed: vocationProfile.BaseAttackSpeed + (stats.Dexterity / AttackSpeedDivisor) + (progress.Level * modifiers.Dexterity / AttackSpeedDivisor),
            CriticalChance: vocationProfile.BaseCriticalChance + (stats.Dexterity / 100) + (progress.Level * modifiers.Dexterity / 100),
            CriticalDamage: vocationProfile.BaseCriticalDamage,
            ManaCostPerAttack: vocationProfile.ManaCostPerAttack,
            DamageType: vocationProfile.DamageType);
    }
    
}