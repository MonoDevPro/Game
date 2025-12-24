namespace Game.Domain.Attributes.Stats.ValueObjects;

/// <summary>
/// Atributos primários do personagem.
/// </summary>
public readonly record struct BaseStats(
    int Strength,
    int Dexterity,
    int Intelligence,
    int Constitution,
    int Spirit)
{
    public static BaseStats Zero => default;

    public static BaseStats operator +(BaseStats a, BaseStats b) => new(
        a.Strength + b.Strength,
        a.Dexterity + b.Dexterity,
        a.Intelligence + b.Intelligence,
        a.Constitution + b.Constitution,
        a.Spirit + b.Spirit);

    public static BaseStats operator *(BaseStats baseStats, double factor) => new(
        (int)(baseStats.Strength * factor),
        (int)(baseStats.Dexterity * factor),
        (int)(baseStats.Intelligence * factor),
        (int)(baseStats.Constitution * factor),
        (int)(baseStats.Spirit * factor));
}