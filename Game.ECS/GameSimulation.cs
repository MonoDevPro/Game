using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.DTOs.Game.Npc;
using Game.DTOs.Game.Player;
using Game.ECS.Entities;
using Game.ECS.Events;
using Game.ECS.Schema.Components;
using Game.ECS.Services;
using Game.ECS.Services.Map;
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
    
    // Index para busca rápida de entidades por NetworkId
    protected readonly EntityIndex<int> PlayerIndex = new();
    protected readonly EntityIndex<int> NPCIndex = new();
    
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
    /// Registers a map with the map service.
    /// </summary>
    public void RegisterMap(int mapId, IMapGrid mapGrid, IMapSpatial mapSpatial) =>
        MapIndex.RegisterMap(mapId, mapGrid, mapSpatial);

    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    protected abstract void ConfigureSystems(World world, Group<float> systems, ILoggerFactory? loggerFactory = null);
    
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
    
    public Entity CreatePlayer(ref PlayerData playerSnapshot)
    {
        var entity = World.CreatePlayer(ref playerSnapshot);
        RegisterSpatialAnchor(entity);
        PlayerIndex.Register(playerSnapshot.NetworkId, entity);
        return entity;
    }
    
    public Entity CreateNpc(ref NpcData snapshot, ref Behaviour behaviour)
    {
        // Atualiza o template com a localização de spawn e networkId
        var entity = World.CreateNpc(ref snapshot, ref behaviour);
        RegisterSpatialAnchor(entity);
        NPCIndex.Register(snapshot.NetworkId, entity);
        return entity;
    }
    
    /// <summary>
    /// Tenta obter a entidade de um jogador pelo NetworkId.
    /// </summary>
    public bool TryGetPlayerEntity(int networkId, out Entity entity) =>
        PlayerIndex.TryGetEntity(networkId, out entity);
    
    /// <summary>
    /// Tenta obter a entidade de um NPC pelo NetworkId.
    /// </summary>
    public bool TryGetNpcEntity(int networkId, out Entity entity) =>
        NPCIndex.TryGetEntity(networkId, out entity);
    
    /// <summary>
    /// Destrói a entidade de um jogador pelo NetworkId.
    /// </summary>
    public virtual bool DestroyPlayer(int networkId)
    {
        if (!PlayerIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        PlayerIndex.RemoveByKey(networkId);
        UnregisterSpatialAnchor(entity);
        World.Destroy(entity);
        return true;
    }
    
    /// <summary>
    /// Destrói a entidade de um NPC pelo NetworkId.
    /// </summary>
    public virtual bool DestroyNpc(int networkId)
    {
        if (!NPCIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        NPCIndex.RemoveByKey(networkId);
        UnregisterSpatialAnchor(entity);
        World.Destroy(entity);
        return true;
    }

    public virtual bool ApplyPlayerState(ref StateData snapshot)
    {
        if (!TryGetPlayerEntity(snapshot.NetworkId, out var entity))
            return false;
        
        World.UpdateState(entity, ref snapshot);
        return true;
    }

    /// <summary>
    /// Garante que a entidade esteja registrada no índice espacial e sincroniza o anchor inicial.
    /// </summary>
    private void RegisterSpatialAnchor(Entity entity)
    {
        if (!World.Has<SpatialAnchor>(entity) || !World.Has<MapId>(entity) || !World.Has<Position>(entity) || !World.Has<Floor>(entity))
            return;

        ref var position = ref World.Get<Position>(entity);
        ref var floor = ref World.Get<Floor>(entity);
        ref var mapId = ref World.Get<MapId>(entity);
        ref var anchor = ref World.Get<SpatialAnchor>(entity);

        try
        {
            MapIndex.GetMapSpatial(mapId.Value).Insert(position, floor.Value, entity);
            anchor.MapId = mapId.Value;
            anchor.Position = position;
            anchor.Floor = floor.Value;
            anchor.IsTracked = true;
        }
        catch (KeyNotFoundException)
        {
            LogWarning("[GameSimulation] Failed to register spatial anchor for entity {Entity}: map {MapId} is not registered", entity, mapId.Value);
        }
    }

    /// <summary>
    /// Remove a entidade do índice espacial, evitando células fantasmas após destruição.
    /// </summary>
    private void UnregisterSpatialAnchor(Entity entity)
    {
        if (!World.Has<SpatialAnchor>(entity))
            return;

        ref var anchor = ref World.Get<SpatialAnchor>(entity);
        if (!anchor.IsTracked)
            return;

        if (!MapIndex.HasMap(anchor.MapId))
            return;

        MapIndex.GetMapSpatial(anchor.MapId).Remove(anchor.Position, anchor.Floor, entity);
        anchor.IsTracked = false;
    }
}