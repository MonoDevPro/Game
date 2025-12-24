namespace GameECS.Shared.Entities.Components;

/// <summary>
/// Área de interesse da entidade.
/// </summary>
public struct AreaOfInterest
{
    public int ViewRadius;
    public long LastUpdateTick;

    public AreaOfInterest(int viewRadius)
    {
        ViewRadius = viewRadius;
        LastUpdateTick = 0;
    }
}

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

/// <summary>
/// Tag: Entidade invisível (não aparece para outros).
/// </summary>
public struct Invisible { }

/// <summary>
/// Tag: Entidade oculta (GM hide).
/// </summary>
public struct Hidden { }
