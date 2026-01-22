using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.ECS.Systems;
using Microsoft.Extensions.Logging;

namespace Game.ECS;

/// <summary>
/// Implementa um timestep fixo para simulação determinística.
/// Acumula delta times e executa updates em intervalos fixos.
/// </summary>
public class FixedTimeStep(float fixedDeltaTime)
{
    private float _accumulator;

    /// <summary>
    /// Acumula tempo delta. Limita a 0.25s para evitar "spiral of death".
    /// </summary>
    public void Accumulate(float deltaTime)
    {
        _accumulator += Math.Min(deltaTime, 0.25f);
    }

    /// <summary>
    /// Verifica se um update deve ser executado.
    /// </summary>
    public bool ShouldUpdate()
    {
        return _accumulator >= fixedDeltaTime;
    }

    /// <summary>
    /// Consome um timestep do acumulador.
    /// </summary>
    public void Step()
    {
        _accumulator -= fixedDeltaTime;
    }
}

/// <summary>
/// Base abstrata para a simulação do jogo usando ECS.
/// Gerencia o World (mundo de entidades), systems (sistemas) e o loop de simulação com timestep fixo.
/// Pode ser usado tanto como server (full simulation) quanto client (partial simulation).
/// </summary>
public abstract class GameSimulation(ILogger<GameSimulation>? logger = null) : GameSystem(World.Create(
        chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
        minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
        archetypeCapacity: SimulationConfig.ArchetypeCapacity,
        entityCapacity: SimulationConfig.EntityCapacity), logger)
{
    
    /// Sistemas ECS da simulação.
    protected readonly Group<float> Systems = new(SimulationConfig.SimulationName);
    
    /// Fixed timestep para updates da simulação.
    private readonly FixedTimeStep _fixedTimeStep = new(SimulationConfig.TickDelta);
    
    /// Tick atual da simulação.
    private uint CurrentTick { get; set; }

    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    protected abstract void ConfigureSystems(World world, Group<float> systems);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Update(in float deltaTime)
    {
        _fixedTimeStep.Accumulate(deltaTime);
    
        while (_fixedTimeStep.ShouldUpdate())
        {
            CurrentTick++;

            Systems.BeforeUpdate(SimulationConfig.TickDelta);

            Systems.Update(SimulationConfig.TickDelta);

            Systems.AfterUpdate(SimulationConfig.TickDelta);

            _fixedTimeStep.Step();
        }
    }

    public override void Dispose()
    {
        Systems.Dispose();
        base.Dispose();
    }
}