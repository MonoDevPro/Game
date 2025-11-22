using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.ECS.Components;
using Game.ECS.Entities.Repositories;
using Game.ECS.Services;
using Game.ECS.Systems;

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
public abstract class GameSimulation : GameSystem
{
    protected readonly Group<float> Systems;
    private readonly FixedTimeStep _fixedTimeStep;
    public uint CurrentTick { get; private set; }
    
    protected IMapService? MapService;
    protected PlayerIndex PlayerIndex { get; } = new();
    protected NpcIndex NpcIndex { get; } = new();
    
    protected GameSimulation(IMapService? mapService = null) : this(
        World.Create(chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
        minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
        archetypeCapacity: SimulationConfig.ArchetypeCapacity,
        entityCapacity: SimulationConfig.EntityCapacity), mapService) { }   

    private GameSimulation(World world, IMapService? mapService) : base(world)
    {
        Systems = new Group<float>(SimulationConfig.SimulationName);
        _fixedTimeStep = new FixedTimeStep(SimulationConfig.TickDelta);
        
        MapService = mapService;
    }

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

    public void RegisterMap(int mapId, IMapGrid grid, IMapSpatial spatial)
    {
        MapService ??= new MapService();

        MapService.RegisterMap(mapId, grid, spatial);
    }

    public void UnregisterMap(int mapId)
    {
        if (MapService == null)
            return;
        
        MapService.UnregisterMap(mapId);
    }
    
    public override void Dispose()
    {
        Systems.Dispose();
        PlayerIndex.Clear();
        NpcIndex.Clear();
        base.Dispose();
    }
}