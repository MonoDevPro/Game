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
    
    protected readonly IMapService MapService;
    
    protected readonly EventBus EventBus = new();
    
    protected readonly PlayerIndex PlayerIndex = new();
    
    protected GameSimulation(GameEventSystem eventSystem, IMapService mapService) : this(
        World.Create(chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
        minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
        archetypeCapacity: SimulationConfig.ArchetypeCapacity,
        entityCapacity: SimulationConfig.EntityCapacity), eventSystem, mapService) { }   

    private GameSimulation(World world, GameEventSystem eventSystem, IMapService mapService) : base(world, eventSystem)
    {
        Systems = new Group<float>(SimulationConfig.SimulationName);
        _fixedTimeStep = new FixedTimeStep(SimulationConfig.TickDelta);
        MapService = mapService;
    }

    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    protected abstract void ConfigureSystems(World world, GameEventSystem eventSystem, Group<float> systems);

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
    public override void BeforeUpdate(in float t) => throw new NotImplementedException();
    public override void AfterUpdate(in float t) => throw new NotImplementedException();
    
    public virtual bool ApplyPlayerInput(Entity entity, sbyte inputX, sbyte inputY, InputFlags flags)
    {
        if (!World.IsAlive(entity) || !World.Has<PlayerControlled>(entity))
            return false;

        var input = new PlayerInput { InputX = inputX, InputY = inputY, Flags = flags };
        World.Set(entity, input);
        return true;
    }
    
    public void RegisterSpatial(Entity entity)
    {
        if (!World.Has<Position>(entity))
            return;

        int mapId = 0;
        if (World.Has<MapId>(entity))
        {
            ref MapId mapComponent = ref World.Get<MapId>(entity);
            mapId = mapComponent.Value;
        }

        if (!MapService.HasMap(mapId))
        {
            MapService.RegisterMap(mapId, new MapGrid(100, 100), new MapSpatial());
        }

        var spatial = MapService.GetMapSpatial(mapId);
        ref Position position = ref World.Get<Position>(entity);
        spatial.Insert(position, entity);
    }

    public void UnregisterSpatial(Entity entity)
    {
        if (!World.Has<Position>(entity))
            return;

        int mapId = 0;
        if (World.Has<MapId>(entity))
        {
            ref MapId mapComponent = ref World.Get<MapId>(entity);
            mapId = mapComponent.Value;
        }

        if (!MapService.HasMap(mapId))
            return;

        var spatial = MapService.GetMapSpatial(mapId);
        ref Position position = ref World.Get<Position>(entity);
        spatial.Remove(position, entity);
    }

    public void RegisterMap(int mapId, IMapGrid grid, IMapSpatial spatial)
    {
        MapService.RegisterMap(mapId, grid, spatial);
    }

    public void UnregisterMap(int mapId)
    {
        MapService.UnregisterMap(mapId);
    }
}