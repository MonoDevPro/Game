using Game.Domain.ValueObjects.Attributes;
using Game.Domain.ValueObjects.Vitals;
using Game.Domain.ValueObjects.Character;
using Game.Domain.Player;

namespace Game.Domain.Attributes;

/// <summary>
/// Calculador de atributos do personagem.
/// Centraliza todas as fórmulas de cálculo do domínio.
/// </summary>
public static class AttributeCalculator
{
    private const int HpPerConstitution = 10;
    private const int HpPerLevel = 5;
    private const int MpPerIntelligence = 5;
    private const int MpPerLevel = 3;
    private const int MinRegenPerTick = 1;
    private const int RegenDivisor = 10;
    private const float BaseSpeed = 1.0f;
    private const float AttackSpeedDivisor = 100.0f;
    private const float MovementSpeedDivisor = 200.0f;

    public static (Health Health, Mana Mana) CalculateVitals(BaseStats total, ValueObjects.Character.Progress progress, StatsModifier modifiers, int currentHp = -1, int currentMp = -1)
    {
        var maxHp = (int)modifiers.ApplyConstitution(HpPerConstitution * total.Constitution + progress.Level * HpPerLevel);
        var maxMp = (int)modifiers.ApplyIntelligence(MpPerIntelligence * total.Intelligence + progress.Level * MpPerLevel);
        
        (int hpRegenPerTick, int mpRegenPerTick) = CalculateRecovery(total);

        return new(
            new Health(
                max: maxHp, 
                regenPerTick: hpRegenPerTick),
            new Mana(
                max: maxMp, 
                regenPerTick: mpRegenPerTick));
    }

    private static (int HpRegenPerTick, int MpRegenPerTick) CalculateRecovery(BaseStats total) => new(
        Math.Max(MinRegenPerTick, total.Constitution / RegenDivisor),
        Math.Max(MinRegenPerTick, total.Spirit / RegenDivisor));
    
    public static ValueObjects.Attributes.Stats CalculateDerived(BaseStats total, ValueObjects.Character.Progress progress, StatsModifier modifiers) => new(
        BaseHealth: (int)modifiers.ApplyConstitution(HpPerConstitution * total.Constitution + progress.Level * HpPerLevel),
        BaseMana: (int)modifiers.ApplyIntelligence(MpPerIntelligence * total.Intelligence + progress.Level * MpPerLevel),
        PhysicalDamage: (int)modifiers.ApplyStrength(2 * total.Strength + progress.Level),
        MagicDamage: (int)modifiers.ApplyIntelligence(3 * total.Intelligence + total.Spirit / 2),
        PhysicalDefense: (int)modifiers.ApplyConstitution(total.Constitution + total.Strength / 2),
        MagicDefense: (int)modifiers.ApplySpirit(total.Spirit + total.Intelligence / 2),
        ManaCostPerAttack: 0,
        CriticalChance: 0f,
        AttackRange: 0f,
        AttackSpeed: BaseSpeed + modifiers.ApplyAttackSpeed(total.Dexterity / AttackSpeedDivisor),
        MovementSpeed: BaseSpeed + modifiers.ApplyMovementSpeed(total.Dexterity / MovementSpeedDivisor));
    
}