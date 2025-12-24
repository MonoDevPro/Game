namespace Game.Domain.Player;

/// <summary>
/// Atributos primários do personagem.
/// </summary>
public readonly record struct Stats(
    int Strength,
    int Dexterity,
    int Intelligence,
    int Constitution,
    int Spirit)
{
    public static Stats Zero => default;

    public static Stats operator +(Stats a, Stats b) => new(
        a.Strength + b.Strength,
        a.Dexterity + b.Dexterity,
        a.Intelligence + b.Intelligence,
        a.Constitution + b.Constitution,
        a.Spirit + b.Spirit);

    public static Stats operator *(Stats stats, double factor) => new(
        (int)(stats.Strength * factor),
        (int)(stats.Dexterity * factor),
        (int)(stats.Intelligence * factor),
        (int)(stats.Constitution * factor),
        (int)(stats.Spirit * factor));
}

/// <summary>
/// Modificadores percentuais aplicados aos atributos.
/// </summary>
public readonly record struct StatsModifier(
    double StrengthFactor,
    double DexterityFactor,
    double IntelligenceFactor,
    double ConstitutionFactor,
    double SpiritFactor)
{
    public static StatsModifier Default => new(1.0, 1.0, 1.0, 1.0, 1.0);

    public double ApplyStrength(double value) => value * StrengthFactor;
    public double ApplyDexterity(double value) => value * DexterityFactor;
    public double ApplyIntelligence(double value) => value * IntelligenceFactor;
    public double ApplyConstitution(double value) => value * ConstitutionFactor;
    public double ApplySpirit(double value) => value * SpiritFactor;
}

/// <summary>
/// Atributos derivados calculados a partir dos stats base.
/// </summary>
public readonly record struct DerivedStats(
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense,
    double AttackSpeed,
    double MovementSpeed)
{
    public static DerivedStats Zero => default;
}

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
    private const double BaseSpeed = 1.0;
    private const double AttackSpeedDivisor = 100.0;
    private const double MovementSpeedDivisor = 200.0;

    public static Vitals CalculateVitals(Stats total, Progress progress, StatsModifier modifiers, int currentHp = -1, int currentMp = -1)
    {
        var maxHp = (int)modifiers.ApplyConstitution(HpPerConstitution * total.Constitution + progress.Level * HpPerLevel);
        var maxMp = (int)modifiers.ApplyIntelligence(MpPerIntelligence * total.Intelligence + progress.Level * MpPerLevel);

        return new Vitals(
            Hp: currentHp < 0 ? maxHp : Math.Min(currentHp, maxHp),
            Mp: currentMp < 0 ? maxMp : Math.Min(currentMp, maxMp),
            MaxHp: maxHp,
            MaxMp: maxMp);
    }

    public static VitalRecovery CalculateRecovery(Stats total) => new(
        HpRegenPerTick: Math.Max(MinRegenPerTick, total.Constitution / RegenDivisor),
        MpRegenPerTick: Math.Max(MinRegenPerTick, total.Spirit / RegenDivisor));

    public static DerivedStats CalculateDerived(Stats total, Progress progress, StatsModifier modifiers) => new(
        PhysicalAttack: (int)modifiers.ApplyStrength(2 * total.Strength + progress.Level),
        MagicAttack: (int)modifiers.ApplyIntelligence(3 * total.Intelligence + total.Spirit / 2),
        PhysicalDefense: (int)modifiers.ApplyConstitution(total.Constitution + total.Strength / 2),
        MagicDefense: (int)modifiers.ApplySpirit(total.Spirit + total.Intelligence / 2),
        AttackSpeed: BaseSpeed + modifiers.ApplyDexterity(total.Dexterity / AttackSpeedDivisor),
        MovementSpeed: BaseSpeed + modifiers.ApplyDexterity(total.Dexterity / MovementSpeedDivisor));
}

/// <summary>
/// Agregado completo de atributos do personagem.
/// Imutável e pronto para uso na camada ECS.
/// </summary>
public sealed class Attributes
{
    public Progress Progress { get; }
    public Vitals Vitals { get; }
    public VitalRecovery Recovery { get; }
    public Stats Base { get; }
    public Stats Bonus { get; }
    public Stats Total { get; }
    public StatsModifier Modifiers { get; }
    public DerivedStats Derived { get; }

    private Attributes(
        Progress progress,
        Vitals vitals,
        VitalRecovery recovery,
        Stats baseStats,
        Stats bonus,
        StatsModifier modifiers,
        DerivedStats derived)
    {
        Progress = progress;
        Base = baseStats;
        Bonus = bonus;
        Total = baseStats + bonus;
        Modifiers = modifiers;
        Vitals = vitals;
        Recovery = recovery;
        Derived = derived;
    }

    /// <summary>
    /// Cria um novo CharacterAttributes calculando todos os valores derivados.
    /// </summary>
    public static Attributes Create(
        Progress progress,
        Stats baseStats,
        Stats bonus = default,
        StatsModifier? modifiers = null,
        int? currentHp = null,
        int? currentMp = null)
    {
        var mods = modifiers ?? StatsModifier.Default;
        var total = baseStats + bonus;
        var vitals = AttributeCalculator.CalculateVitals(total, progress, mods, currentHp ?? -1, currentMp ?? -1);
        var recovery = AttributeCalculator.CalculateRecovery(total);
        var derived = AttributeCalculator.CalculateDerived(total, progress, mods);

        return new Attributes(progress, vitals, recovery, baseStats, bonus, mods, derived);
    }

    /// <summary>
    /// Recalcula atributos mantendo HP/MP atuais (útil para level up ou mudança de equipamentos).
    /// </summary>
    public Attributes Recalculate(
        Stats? newBase = null,
        Stats? newBonus = null,
        StatsModifier? newModifiers = null,
        Progress? newProgress = null)
    {
        return Create(
            newProgress ?? Progress,
            newBase ?? Base,
            newBonus ?? Bonus,
            newModifiers ?? Modifiers,
            Vitals.Hp,
            Vitals.Mp);
    }

    /// <summary>
    /// Retorna uma cópia com os vitals atualizados.
    /// </summary>
    public Attributes WithVitals(Vitals newVitals) =>
        new(Progress, newVitals, Recovery, Base, Bonus, Modifiers, Derived);
}
