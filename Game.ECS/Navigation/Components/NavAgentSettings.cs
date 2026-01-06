using System.Runtime.CompilerServices;

namespace Game.ECS.Navigation.Components;

/// <summary>
/// Configuração do agente - SERVIDOR.
/// Usa ticks ao invés de tempo. 
/// </summary>
public struct NavAgentSettings
{
    public int MoveDurationTicks;        // Ticks para mover 1 célula
    public int DiagonalDurationTicks;    // Ticks para diagonal
    public bool AllowDiagonal;
    public byte MaxPathRetries;

    public static NavAgentSettings Default => new()
    {
        MoveDurationTicks = 6,           // ~100ms a 60 ticks/s
        DiagonalDurationTicks = 9,       // ~141ms
        AllowDiagonal = true,
        MaxPathRetries = 3
    };

    public static NavAgentSettings Slow => new()
    {
        MoveDurationTicks = 12,
        DiagonalDurationTicks = 17,
        AllowDiagonal = true,
        MaxPathRetries = 3
    };

    public static NavAgentSettings Fast => new()
    {
        MoveDurationTicks = 3,
        DiagonalDurationTicks = 4,
        AllowDiagonal = true,
        MaxPathRetries = 3
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetDuration(bool isDiagonal)
        => isDiagonal ? DiagonalDurationTicks : MoveDurationTicks;
}