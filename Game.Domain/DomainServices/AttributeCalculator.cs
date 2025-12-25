using Game.Domain.ValueObjects.Attributes;
using Game.Domain.ValueObjects.Vitals;
using Game.Domain.ValueObjects.Character;
using Game.Domain.Entities;

namespace Game.Domain.DomainServices;

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
    private const float BaseCriticalChance = 5.0f;
    private const float MaxCriticalChance = 75.0f;
    
    public static (Health Health, Mana Mana) CalculateVitals(BaseStats total, Progress progress, BaseStats modifiers, double currentHp = -1, double currentMp = -1)
    {
        var maxHp = HpPerConstitution * total.Constitution + progress.Level * HpPerLevel;
        var maxMp = MpPerIntelligence * total.Intelligence + progress.Level * MpPerLevel;
        var hpRegenPerTick = Math.Max(MinRegenPerTick, total.Constitution / RegenDivisor);
        var mpRegenPerTick= Math.Max(MinRegenPerTick, total.Spirit / RegenDivisor);

        return new(
            new Health(
                max: maxHp, 
                regenPerTick: hpRegenPerTick),
            new Mana(
                max: maxMp, 
                regenPerTick: mpRegenPerTick));
    }

    public static Stats CalculateStats(BaseStats stats, Progress progress, BaseStats modifiers)
       => new(
            PhysicalAttack: stats.Strength * 2 + progress.Level + modifiers.Strength,
            MagicAttack: stats.Intelligence * 2 + progress.Level + modifiers.Intelligence,
            PhysicalDefense: stats.Constitution + progress.Level + modifiers.Constitution,
            MagicDefense: stats.Spirit + progress.Level + modifiers.Spirit,
            AttackRange: 1 + stats.Dexterity / 10 + modifiers.Dexterity,
            AttackSpeed: BaseSpeed + (stats.Dexterity + modifiers.Dexterity) / AttackSpeedDivisor,
            MovementSpeed: BaseSpeed + (stats.Dexterity + modifiers.Dexterity) / MovementSpeedDivisor,
            CriticalChance: Math.Min(
                MaxCriticalChance, 
                BaseCriticalChance + (stats.Dexterity + modifiers.Dexterity) / 5)
        );
    
    
    
}