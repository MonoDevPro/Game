namespace Game.Infrastructure.ArchECS;

public static class SimulationConfig
{
    public const string SimulationName = "GameSimulation";

    // World chunking configuration
    public const int ChunkSizeInBytes = 16_384;
    public const int MinimumAmountOfEntitiesPerChunk = 100;
    public const int ArchetypeCapacity = 2;
    public const int EntityCapacity = 64;
    
    // Ticks de simulação por segundo (fixo e determinístico)
    public const int TicksPerSecond = 60;
    public const int TickDeltaMilliseconds = 1000 / TicksPerSecond;
}