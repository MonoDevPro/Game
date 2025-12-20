namespace Game.ECS.Navigation.Shared.Data;

/// <summary>
/// Configuração do sistema de navegação.
/// </summary>
public sealed class NavigationConfig
{
    public int MaxNodesPerSearch { get; init; } = 2048;
    public int MaxPathLength { get; init; } = 256;
    public int MaxRequestsPerTick { get; init; } = 50;
    public int PoolPrewarmCount { get; init; } = 8;
    public bool EnableDiagonalMovement { get; init; } = true;
    public bool PreventCornerCutting { get; init; } = true;

    public static NavigationConfig Default => new();

    public static NavigationConfig HighPerformance => new()
    {
        MaxNodesPerSearch = 1024,
        MaxRequestsPerTick = 100,
        PoolPrewarmCount = 16
    };

    public static NavigationConfig HighQuality => new()
    {
        MaxNodesPerSearch = 4096,
        MaxRequestsPerTick = 25,
        PreventCornerCutting = true
    };
}