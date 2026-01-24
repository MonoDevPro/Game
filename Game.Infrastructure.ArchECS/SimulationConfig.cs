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

    // Tamanho de uma célula do mundo em unidades lógicas (não pixels)
    public const int CellSize = 1;
    
    // Configurações padrão de respawn
    public const int DefaultRespawnTime = 5000; // em milissegundos
    public const int ReviveHealthPercent = 50; // 50% HP
    public const int ReviveManaPercent = 50; // 50% MP
    
    /// <summary>
    /// Tempo que o jogador precisa ficar sem tomar dano
    /// para que a regeneração de HP/MP volte a funcionar.
    /// </summary>
    public const int RegenerationDelayAfterDamage = 3000; // em milissegundos
}