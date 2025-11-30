using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Events;
using Game.ECS.Services;
using Game.ECS.Services.Index;
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
    
    protected readonly GameEventBus EventBus = new();
    
    /// Fixed timestep para updates da simulação.
    private readonly FixedTimeStep _fixedTimeStep = new(SimulationConfig.TickDelta);
    
    /// Tick atual da simulação.
    private uint CurrentTick { get; set; }
    
    /// <summary>
    /// Access to the map service for spatial queries.
    /// </summary>
    protected readonly IMapIndex MapIndex = new MapIndex();
    
    /// <summary>
    /// Resource stack for managing string handles.
    /// </summary>
    public readonly ResourceIndex<string> Strings = new(capacity: 10);
    
    /// <summary>
    /// Registers a map with the map service.
    /// </summary>
    public void RegisterMap(int mapId, IMapGrid mapGrid, IMapSpatial mapSpatial) =>
        MapIndex.RegisterMap(mapId, mapGrid, mapSpatial);

    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    protected abstract void ConfigureSystems(World world, Group<float> systems, ILoggerFactory? loggerFactory = null);
    
    #region Player Lifecycle
    
    /// <summary>
    /// Creates a player entity from a template.
    /// </summary>
    public Entity CreatePlayer(PlayerTemplate template)
    {
        var playerEntity = PlayerFactories.CreatePlayer(World, Strings, template);
        
    }
    
    /// <summary>
    /// Destroys a player entity by network ID.
    /// </summary>
    public virtual bool DestroyEntity(int networkId)
    {
        if (!_entityIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        _entityIndex.RemoveByKey(networkId);
        
        if (World.Has<NameHandle>(entity))
        {
            var nameRef = World.Get<NameHandle>(entity);
            Strings.Unregister(nameRef.Value);
        }
        
        World.Destroy(entity);
        return true;
    }
    
    #endregion
    
    #region NPC Lifecycle
    
    /// <summary>
    /// Creates an NPC entity from a template.
    /// </summary>
    public Entity CreateNpc(NpcTemplate template, int networkId, int mapId, int floor, Position position)
    {
        var npcEntity = NpcFactories.CreateNpc(World, template, Strings, networkId, mapId, floor, position);
        _entityIndex.Register(networkId, npcEntity);
        
        return npcEntity;
    }
    
    
    /// <summary>
    /// Destroys an NPC entity by network ID.
    /// </summary>
    public virtual bool DestroyNpc(int networkId)
    {
        if (!NpcIndex.TryGetEntity(networkId, out var entity))
            return false;
            
        NpcLifecycle.DestroyNPC(World, ReleaseStringHandle, entity);
        NpcIndex.Unregister(networkId);
        return true;
    }
    
    #endregion

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
        LoggerFactory.Dispose();
        base.Dispose();
    }
}