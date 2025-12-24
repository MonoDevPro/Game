namespace GameECS.Shared.Entities.Data;

/// <summary>
/// Configuração de party.
/// </summary>
public struct PartyConfig
{
    public int MaxMembers;
    public LootDistribution LootDistribution;
    public ExpDistribution ExpDistribution;
    public int MaxLevelDifference;
    public int MaxDistance;

    public static PartyConfig Default => new()
    {
        MaxMembers = 5,
        LootDistribution = LootDistribution.FreeForAll,
        ExpDistribution = ExpDistribution.Equal,
        MaxLevelDifference = 50,
        MaxDistance = 30
    };
}
