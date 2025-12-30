namespace Game.Domain.Commons.Enums;

/// <summary>
/// Distribuição de loot.
/// </summary>
public enum LootDistribution : byte
{
    FreeForAll = 0,
    RoundRobin = 1,
    Leader = 2,
    NeedBeforeGreed = 3
}