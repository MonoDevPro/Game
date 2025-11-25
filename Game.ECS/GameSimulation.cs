using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.ECS.Components;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Factories;
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
    
    public bool TryGetPlayerEntity(int playerId, out Entity entity) => PlayerIndex.TryGetEntity(playerId, out entity);
    
    public bool TryGetNpcEntity(int networkId, out Entity entity) => NpcIndex.TryGetEntity(networkId, out entity);
    
    public bool TryGetAnyEntity(int networkId, out Entity entity) => 
        PlayerIndex.TryGetEntity(networkId, out entity) || NpcIndex.TryGetEntity(networkId, out entity);

    public void RegisterMap(int mapId, IMapGrid grid, IMapSpatial spatial)
    {
        MapService ??= new MapService();
        MapService.RegisterMap(mapId, grid, spatial);
    }

    public void UnregisterMap(int mapId)
    {
        MapService?.UnregisterMap(mapId);
    }
    
    public Entity CreatePlayer(in PlayerData data)
    {
        if (PlayerIndex.TryGetEntity(data.NetworkId, out _))
            if (!DestroyPlayer(data.NetworkId))
                return Entity.Null;
        
        var entity = World.CreatePlayer(PlayerIndex, data);
        if (World.TryGet(entity, out MapId mapId) && 
            World.TryGet(entity, out Position position) &&
            World.TryGet(entity, out Floor floor))
            Systems.Get<SpatialSyncSystem>()
                .OnEntityCreated(entity, 
                    new SpatialPosition(
                        position.X, 
                        position.Y, 
                        floor.Level), 
                    mapId.Value);
        return entity;
    }

    public Entity CreateNpc(in NPCData data, NpcBehaviorData behaviorData)
    {
        if (NpcIndex.TryGetEntity(data.NetworkId, out var existingEntity))
        {
            if (!DestroyNpc(data.NetworkId))
                return Entity.Null; // Falha ao destruir a entidade existente
        }
        
        var entity = World.CreateNPC(data, behaviorData);
        NpcIndex.AddMapping(data.NetworkId, entity);
        Systems.Get<SpatialSyncSystem>()
            .OnEntityCreated(entity,
                new SpatialPosition(
                    data.PositionX,
                    data.PositionY,
                    data.Floor),
                data.MapId);
        return entity;
    }
    
    public virtual bool DestroyPlayer(int networkId)
    {
        if (!PlayerIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        if (World.TryGet(entity, out MapId mapId) && 
            World.TryGet(entity, out Position position) &&
            World.TryGet(entity, out Floor floor))
            Systems.Get<SpatialSyncSystem>()
                .OnEntityDestroyed(entity, 
                    new SpatialPosition(
                        position.X, 
                        position.Y, 
                        floor.Level),
                    mapId.Value);
        PlayerIndex.RemoveByEntity(entity);
        World.Destroy(entity);
        return true;
    }

    public virtual bool DestroyNpc(int networkId)
    {
        if (!NpcIndex.TryGetEntity(networkId, out var entity))
            return false;
        
        if (World.TryGet(entity, out MapId mapId) &&
            World.TryGet(entity, out Position position) &&
            World.TryGet(entity, out Floor floor))
            Systems.Get<SpatialSyncSystem>()
                .OnEntityDestroyed(entity, 
                    new SpatialPosition(
                        position.X, 
                        position.Y, 
                        floor.Level),
                    mapId.Value);
        NpcIndex.RemoveByKey(networkId);
        World.Destroy(entity);
        return true;
    }
    
    public override void Dispose()
    {
        Systems.Dispose();
        PlayerIndex.Clear();
        NpcIndex.Clear();
        base.Dispose();
    }
}