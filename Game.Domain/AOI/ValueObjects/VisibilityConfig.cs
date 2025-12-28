namespace Game.Domain.AOI.ValueObjects;

/// <summary>
/// Configuração de visibilidade.
/// </summary>
public struct VisibilityConfig
{
    public int ViewRadius;
    public int MaxVisibleEntities;
    public int UpdateRate;

    public static VisibilityConfig ForPlayer => new()
    {
        ViewRadius = 18,
        MaxVisibleEntities = 128,
        UpdateRate = 5
    };

    public static VisibilityConfig ForNpc => new()
    {
        ViewRadius = 10,
        MaxVisibleEntities = 32,
        UpdateRate = 10
    };

    public static VisibilityConfig ForPet => new()
    {
        ViewRadius = 18,
        MaxVisibleEntities = 64,
        UpdateRate = 5
    };
}