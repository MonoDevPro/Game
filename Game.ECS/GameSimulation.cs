using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Archetypes;
using Game.ECS.Entities.Data;
using Game.ECS.Systems;
using Game.ECS.Utils;

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
public abstract class GameSimulation
{
    protected readonly World World;
    protected readonly Group<float> Systems;
    protected readonly GameEventSystem EventSystem;
    protected readonly IEntityFactory EntityFactory;
    
    private readonly FixedTimeStep _fixedTimeStep;

    protected GameSimulation()
    {
        World = World.Create(
            chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: SimulationConfig.ArchetypeCapacity,
            entityCapacity: SimulationConfig.EntityCapacity);
        
        Systems = new Group<float>(SimulationConfig.SimulationName);
        EventSystem = new GameEventSystem();
        EntityFactory = new EntityFactory(World);
        _fixedTimeStep = new FixedTimeStep(SimulationConfig.TickDelta);
    }
    
    /// <summary>
    /// Tick atual da simulação. Incrementa a cada atualização.
    /// </summary>
    public uint CurrentTick { get; private set; }
    
    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    public abstract void ConfigureSystems(World world, Group<float> group);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Update(float deltaTime)
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

    public Entity SpawnPlayer(PlayerCharacter data)
    {
        var entity = EntityFactory.CreatePlayer(data);
        NotifyEntitySpawned(entity);
        return entity;
    }
    
    public Entity SpawnLocalPlayer(PlayerCharacter data) 
    {
        var entity = EntityFactory.CreateLocalPlayer(data);
        NotifyEntitySpawned(entity);
        return entity;
    }
    
    public Entity SpawnRemotePlayer(PlayerCharacter data)
    {
        var entity = EntityFactory.CreateRemotePlayer(data);
        NotifyEntitySpawned(entity);
        return entity;
    }

    public Entity SpawnNpc(NPCCharacter data)
    {
        var entity = EntityFactory.CreateNPC(data);
        NotifyEntitySpawned(entity);
        return entity;
    }

    public Entity SpawnProjectile(ProjectileData data)
    {
        var entity = EntityFactory.CreateProjectile(data);
        NotifyEntitySpawned(entity);
        return entity;
    }

    public Entity SpawnDroppedItem(DroppedItemData data)
    {
        var entity = EntityFactory.CreateDroppedItem(data);
        NotifyEntitySpawned(entity);
        return entity;
    }

    public void DespawnEntity(Entity entity)
    {
        if (World.IsAlive(entity))
        {
            NotifyEntityDespawned(entity);
            World.Destroy(entity);
        }
    }
    
    public bool TryGetPlayerState(Entity entity, 
        out PlayerStateSnapshot snapshot)
    {
        ref NetworkId netId = ref World.Get<NetworkId>(entity);
        ref Position position = ref World.Get<Position>(entity);
        ref Facing facing = ref World.Get<Facing>(entity);
        ref Walkable walkable = ref World.Get<Walkable>(entity);
        snapshot = new PlayerStateSnapshot(
            NetworkId: netId.Value,
            PositionX: position.X,
            PositionY: position.Y,
            PositionZ: position.Z,
            FacingX: facing.DirectionX,
            FacingY: facing.DirectionY,
            Speed: walkable.BaseSpeed * walkable.CurrentModifier);
        return true;
    }

    public bool TryGetPlayerVitals(Entity entity, out PlayerVitalsSnapshot vitals)
    {
        ref NetworkId netId = ref World.Get<NetworkId>(entity);
        ref Health health = ref World.Get<Health>(entity);
        ref Mana mana = ref World.Get<Mana>(entity);
        vitals = new PlayerVitalsSnapshot(
            NetworkId: netId.Value,
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max);
        return true;
    }

    public bool TryApplyPlayerInput(Entity entity, PlayerInput input)
    {
        if (World.IsAlive(entity) && World.Has<PlayerInput>(entity))
        {
            World.Set(entity, input);
            return true;
        }
        return false;
    }

    private void NotifyEntitySpawned(Entity entity)
    {
        EventSystem.RaiseEntitySpawned(entity);

        if (World.Has<PlayerId>(entity) && World.TryGet(entity, out NetworkId netId))
        {
            EventSystem.RaisePlayerJoined(netId.Value);
        }
    }

    private void NotifyEntityDespawned(Entity entity)
    {
        EventSystem.RaiseEntityDespawned(entity);

        if (World.Has<PlayerId>(entity) && World.TryGet(entity, out NetworkId netId))
        {
            EventSystem.RaisePlayerLeft(netId.Value);
        }
    }
}