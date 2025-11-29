using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.LowLevel;
using Arch.System;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Services;
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
public abstract class GameSimulation(ILoggerFactory loggerFactory) : GameSystem(World.Create(
        chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
        minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
        archetypeCapacity: SimulationConfig.ArchetypeCapacity,
        entityCapacity: SimulationConfig.EntityCapacity),
    loggerFactory.CreateLogger<GameSimulation>())
{
    protected readonly Group<float> Systems = new(SimulationConfig.SimulationName);
    private readonly FixedTimeStep _fixedTimeStep = new(SimulationConfig.TickDelta);
    
    public uint CurrentTick { get; private set; }
    protected ILoggerFactory LoggerFactory { get; } = loggerFactory;

    public readonly IEntityIndex<int> EntityIndex = new EntityIndex<int>();
    
    /// <summary>
    /// Access to the map service for spatial queries.
    /// </summary>
    public readonly IMapService MapService = new MapService();
    
    // Banco de Strings (Nomes de NPCs, etc)
    public readonly ResourceStack<string> Strings = new(capacity: 10);
    
    /// <summary>
    /// Registers a map with the map service.
    /// </summary>
    public void RegisterMap(int mapId, IMapGrid mapGrid, IMapSpatial mapSpatial) =>
        MapService.RegisterMap(mapId, mapGrid, mapSpatial);
    
    /// <summary>
    /// Tries to get any entity (player or NPC) by network ID.
    /// </summary>
    public bool TryGetAnyEntity(int networkId, out Entity entity) => 
        EntityIndex.TryGetEntity(networkId, out entity);

    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    protected abstract void ConfigureSystems(World world, Group<float> systems);
    
    #region Player Lifecycle
    
    /// <summary>
    /// Creates a player entity from a template.
    /// </summary>
    public Entity CreatePlayer(PlayerTemplate template)
    {
        // TODO: Generate unique network ID for player,
        // TODO: Register player in PlayerIndex and EntityIndex
        // TODO: Register entity in Services (e.g., spatial map)
        return Entity.Null;
    }
    
    /// <summary>
    /// Destroys a player entity by network ID.
    /// </summary>
    public virtual bool DestroyPlayer(int networkId)
    {
        if (!EntityIndex.TryGetEntity(networkId, out var entity))
            return false;
            
        PlayerFactories.DestroyPlayer(World, entity, Strings);
        EntityIndex.RemoveByKey(networkId);
        return true;
    }
    
    #endregion
    
    #region NPC Lifecycle
    
    /// <summary>
    /// Creates an NPC entity from a template.
    /// </summary>
    public Entity CreateNpc(NpcTemplate template, Position position, int floor, int mapId, int networkId)
    {
        var entity = NpcFactories.CreateNpc(World, Strings, template, position, floor, mapId, networkId);
        NpcIndex.Register(networkId, entity);
        return entity;
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