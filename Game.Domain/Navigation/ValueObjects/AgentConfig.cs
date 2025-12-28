using System.Runtime.CompilerServices;

namespace Game.Domain.Navigation.ValueObjects;

/// <summary>
/// Configuração de agente no servidor.
/// </summary>
public struct AgentConfig
{
    public int CardinalMoveTicks;
    public int DiagonalMoveTicks;
    public bool AllowDiagonal;
    public byte MaxRetries;

    public static AgentConfig Default => new()
    {
        CardinalMoveTicks = 6,   // ~100ms @ 60 ticks/s
        DiagonalMoveTicks = 9,   // ~150ms
        AllowDiagonal = true,
        MaxRetries = 3
    };

    public static AgentConfig Slow => new()
    {
        CardinalMoveTicks = 12,
        DiagonalMoveTicks = 17,
        AllowDiagonal = true,
        MaxRetries = 3
    };

    public static AgentConfig Fast => new()
    {
        CardinalMoveTicks = 3,
        DiagonalMoveTicks = 4,
        AllowDiagonal = true,
        MaxRetries = 3
    };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly int GetMoveTicks(bool diagonal) 
        => diagonal ? DiagonalMoveTicks : CardinalMoveTicks;
}