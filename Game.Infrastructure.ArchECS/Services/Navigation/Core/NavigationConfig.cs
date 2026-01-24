namespace Game.Infrastructure.ArchECS.Services.Navigation.Core;

/// <summary>
/// Configuração global do sistema de navegação. 
/// </summary>
public sealed class NavigationConfig
{
    public int MaxNodesPerSearch { get; set; } = 2048;
    public int MaxPathLength { get; set; } = 256;
    public int MaxRequestsPerTick { get; set; } = 50;
    public int ParallelWorkers { get; set; } = 4;
    public float DefaultSearchTimeout { get; set; } = 0.1f; // 100ms
    public bool EnablePathSmoothing { get; set; } = true;
    public bool EnableDiagonalMovement { get; set; } = true;
    public bool PreventCornerCutting { get; set; } = true;

    public static NavigationConfig Default => new();

    public static NavigationConfig HighPerformance => new()
    {
        MaxNodesPerSearch = 1024,
        MaxRequestsPerTick = 100,
        ParallelWorkers = Environment.ProcessorCount,
        EnablePathSmoothing = false
    };

    public static NavigationConfig HighQuality => new()
    {
        MaxNodesPerSearch = 4096,
        MaxRequestsPerTick = 25,
        EnablePathSmoothing = true,
        PreventCornerCutting = true
    };
}