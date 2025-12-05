using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.ECS.Entities;
using Game.ECS.Schema.Snapshots;
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
    
    public Entity CreatePlayer(ref PlayerSnapshot playerSnapshot)
    {
        var entity = World.CreatePlayer(ref playerSnapshot);
        PlayerIndex.Register(playerSnapshot.NetworkId, entity);
        return entity;
    }
    
    public Entity CreateNpc(ref NpcSnapshot snapshot, ref Behaviour behaviour)
    {
        // Atualiza o template com a localização de spawn e networkId
        var entity = World.CreateNpc(ref snapshot, ref behaviour);
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
        World.Destroy(entity);
        return true;
    }

    public virtual bool ApplyPlayerState(ref StateSnapshot snapshot)
    {
        if (!TryGetPlayerEntity(snapshot.NetworkId, out var entity))
            return false;
        
        World.UpdateState(entity, ref snapshot);
        return true;
    }
}