namespace Game.ECS.Shared;

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
    public const float TickDelta = 1f / TicksPerSecond;

    // Tamanho de uma célula do mundo em unidades lógicas (não pixels)
    public const float CellSize = 1f;
    
    // Configurações padrão de respawn
    public const float ReviveHealthPercent = 0.5f; // 50% HP
    public const float ReviveManaPercent = 0.5f; // 50% MP
    
    /// <summary>
    /// Tempo que o jogador precisa ficar sem tomar dano
    /// para que a regeneração de HP/MP volte a funcionar.
    /// </summary>
    public const float HealthRegenDelayAfterCombat = 5.0f; // por exemplo, 5s
}