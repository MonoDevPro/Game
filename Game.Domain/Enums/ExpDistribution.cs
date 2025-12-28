namespace Game.Domain.Enums;

/// <summary>
/// Distribuição de experiência.
/// </summary>
public enum ExpDistribution : byte
{
    Equal = 0,
    ByDamage = 1,
    ByLevel = 2
}