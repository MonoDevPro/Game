using System.Runtime.InteropServices;

namespace Game.Domain.ValueObjects.Attributes;

/// <summary>
/// Atributos primários do personagem.
/// Component ECS representando os 5 atributos base.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct BaseStats(
    double Strength,
    double Dexterity,
    double Intelligence,
    double Constitution,
    double Spirit)
{
    public static BaseStats Zero => default;

    public static BaseStats operator +(BaseStats a, BaseStats b) => new(
        a.Strength + b.Strength,
        a.Dexterity + b.Dexterity,
        a.Intelligence + b.Intelligence,
        a.Constitution + b.Constitution,
        a.Spirit + b.Spirit);

    public static BaseStats operator *(BaseStats baseStats, double factor) => new(
        baseStats.Strength * factor,
        baseStats.Dexterity * factor,
        baseStats.Intelligence * factor,
        baseStats.Constitution * factor,
        baseStats.Spirit * factor);
    
    public static BaseStats operator *(BaseStats baseStats, BaseStats factor) => new(
        baseStats.Strength * factor.Strength,
        baseStats.Dexterity * factor.Dexterity,
        baseStats.Intelligence * factor.Intelligence,
        baseStats.Constitution * factor.Constitution,
        baseStats.Spirit * factor.Spirit);
    
}