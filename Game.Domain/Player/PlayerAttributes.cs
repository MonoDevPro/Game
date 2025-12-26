using Game.Domain.DomainServices;
using Game.Domain.ValueObjects.Character;
using Game.Domain.ValueObjects.Attributes;
using Game.Domain.ValueObjects.Vitals;

namespace Game.Domain.Player;

/// <summary>
/// Agregado completo de atributos do personagem.
/// Imutável e pronto para uso na camada ECS.
/// </summary>
public sealed class PlayerAttributes
{
    public Vocation Vocation { get; }
    public Progress Progress { get; }
    public Health Hp { get; }
    public Mana Mp { get; }
    public BaseStats Stats { get; }
    public BaseStats Bonus { get; }
    public CombatStats Derived { get; }

    private PlayerAttributes(
        Vocation vocation,
        Health hp,
        Mana mp,
        Progress progress,
        BaseStats baseStats,
        BaseStats bonus,
        BaseStats modifiers,
        CombatStats derived)
    {
        Vocation = vocation;
        Hp = hp;
        Mp = mp;
        Progress = progress;
        Bonus = bonus;
        Stats = baseStats + bonus;
        Derived = derived;
    }

    /// <summary>
    /// Cria um novo CharacterAttributes calculando todos os valores derivados.
    /// </summary>
    public static PlayerAttributes Create(
        Vocation vocation,
        Progress progress,
        BaseStats total)
    {
        var vitals = AttributeCalculator.CalculateVitals(total, progress);
        var derived = AttributeCalculator.CalculateCombatStats(total, progress, vocation);

        return new PlayerAttributes(
            vocation,
            vitals.Health,
            vitals.Mana,
            progress,
            total,
            BaseStats.Zero,
            BaseStats.Zero,
            derived);
    }

    /// <summary>
    /// Recalcula atributos mantendo HP/MP atuais (útil para level up ou mudança de equipamentos).
    /// </summary>
    public PlayerAttributes Recalculate(
        BaseStats? newBase = null,
        BaseStats? newBonus = null,
        Progress? newProgress = null)
    {
        var bonusStats = newBonus ?? Bonus;
        var progress = newProgress ?? Progress;
        var totalStats = (newBase ?? Stats) + bonusStats;

        var vitals = AttributeCalculator.CalculateVitals(totalStats, progress);
        var derived = AttributeCalculator.CalculateCombatStats(totalStats, progress, Vocation);

        // Mantém o HP/MP atuais proporcionais ao novo máximo
        var currentHp = (Hp.Current / Hp.Maximum * vitals.Health.Maximum);
        var currentMp = (Mp.Current / Mp.Maximum * vitals.Mana.Maximum);

        return new PlayerAttributes(
            Vocation,
            vitals.Health.WithCurrent(currentHp),
            vitals.Mana.WithCurrent(currentMp),
            progress,
            
            bonusStats,
            modifierStats,
            derived);
    }

    /// <summary>
    /// Retorna uma cópia com os vitals atualizados.
    /// </summary>
    public PlayerAttributes WithVitals((Health Hp, Mana Mp) newVitals) =>
        new(Progress, newVitals, Base, Bonus, Modifiers, Derived);
}
