namespace Simulation.Core.Shared.Options;

public class GameLoopOptions
{
    public const string SectionName = "GameLoop";

    /// <summary>
    /// A frequência de atualização da simulação em Ticks por Segundo (TPS).
    /// </summary>
    public int TicksPerSecond { get; set; } = 60;

    /// <summary>
    /// O valor máximo para o delta time em segundos, para evitar a "espiral da morte"
    /// em caso de picos de lag.
    /// </summary>
    public double MaxDeltaTime { get; set; } = 0.25;
}