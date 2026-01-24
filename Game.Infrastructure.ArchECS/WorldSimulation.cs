using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.Infrastructure.ArchECS.Commons.Systems;
using Game.Infrastructure.ArchECS.Services.Navigation;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.ArchECS;

/// <summary>
/// Base abstrata para a simulação do jogo usando ECS.
/// Gerencia o World (mundo de entidades), systems (sistemas) e o loop de simulação com timestep fixo.
/// Pode ser usado tanto como server (full simulation) quanto client (partial simulation).
/// </summary>
public abstract class WorldSimulation(ILogger<WorldSimulation>? logger = null) : GameSystem(World.Create(
        chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
        minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
        archetypeCapacity: SimulationConfig.ArchetypeCapacity,
        entityCapacity: SimulationConfig.EntityCapacity), logger)
{
    
    /// Sistemas ECS da simulação.
    protected readonly Group<float> Systems = new(SimulationConfig.SimulationName);
    
    /// Fixed timestep para updates da simulação.
    private readonly FixedTimeStep _fixedTimeStep = new(SimulationConfig.TickDeltaMilliseconds);
    
    /// Tick atual da simulação.
    private uint CurrentTick { get; set; }

    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    protected abstract void ConfigureSystems(World world, Group<float> systems);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Update(in long deltaTime)
    {
        _fixedTimeStep.Accumulate(deltaTime);
        
        while (_fixedTimeStep.ShouldUpdate())
        {
            CurrentTick++;

            Systems.BeforeUpdate(SimulationConfig.TickDeltaMilliseconds);

            Systems.Update(SimulationConfig.TickDeltaMilliseconds);

            Systems.AfterUpdate(SimulationConfig.TickDeltaMilliseconds);

            _fixedTimeStep.Step();
        }
    }

    public override void Dispose()
    {
        Systems.Dispose();
        base.Dispose();
    }
}