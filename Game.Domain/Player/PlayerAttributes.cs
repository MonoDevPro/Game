using Game.Domain.Attributes;
using Game.Domain.Attributes.Progress.ValueObjects;
using Game.Domain.Attributes.Stats.ValueObjects;
using Game.Domain.Attributes.Vitals.ValueObjects;

namespace Game.Domain.Player;

/// <summary>
/// Agregado completo de atributos do personagem.
/// Imutável e pronto para uso na camada ECS.
/// </summary>
public sealed class PlayerAttributes
{
    public Progress Progress { get; }
    public (Health Hp, Mana Mp) Vitals { get; }
    public BaseStats Base { get; }
    public BaseStats Bonus { get; }
    public BaseStats Total { get; }
    public StatsModifier Modifiers { get; }
    public Stats Derived { get; }

    private PlayerAttributes(
        Progress progress,
        (Health Hp, Mana Mp) vitals,
        BaseStats baseStats,
        BaseStats bonus,
        StatsModifier modifiers,
        Stats derived)
    {
        Progress = progress;
        Vitals = vitals;
        Base = baseStats;
        Bonus = bonus;
        Modifiers = modifiers;
        Total = baseStats + bonus;
        Derived = derived;
    }

    /// <summary>
    /// Cria um novo CharacterAttributes calculando todos os valores derivados.
    /// </summary>
    public static PlayerAttributes Create(
        Progress progress,
        BaseStats baseStats,
        BaseStats bonus = default,
        StatsModifier? modifiers = null,
        int? currentHp = null,
        int? currentMp = null)
    {
        var mods = modifiers ?? StatsModifier.Default;
        var total = baseStats;
        var vitals = AttributeCalculator.CalculateVitals(total, progress, mods, currentHp ?? -1, currentMp ?? -1);
        var derived = AttributeCalculator.CalculateDerived(total, progress, mods);

        return new PlayerAttributes(progress, vitals, baseStats, bonus, mods, derived);
    }

    /// <summary>
    /// Recalcula atributos mantendo HP/MP atuais (útil para level up ou mudança de equipamentos).
    /// </summary>
    public PlayerAttributes Recalculate(
        BaseStats? newBase = null,
        BaseStats? newBonus = null,
        StatsModifier? newModifiers = null,
        Progress? newProgress = null)
    {
        return Create(
            newProgress ?? Progress,
            newBase ?? Base,
            newBonus ?? Bonus,
            newModifiers ?? Modifiers,
            Vitals.Hp.Current,
            Vitals.Mp.Current);
    }

    /// <summary>
    /// Retorna uma cópia com os vitals atualizados.
    /// </summary>
    public PlayerAttributes WithVitals((Health Hp, Mana Mp) newVitals) =>
        new(Progress, newVitals, Base, Bonus, Modifiers, Derived);
}
