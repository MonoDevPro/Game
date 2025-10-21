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

public class FixedTimeStep(float fixedDeltaTime)
{
    private float _accumulator;

    public void Accumulate(float deltaTime)
    {
        _accumulator += Math.Min(deltaTime, 0.25f); // Prevenir spiral of death
    }

    public bool ShouldUpdate()
    {
        return _accumulator >= fixedDeltaTime;
    }

    public void Step()
    {
        _accumulator -= fixedDeltaTime;
    }
}

public abstract class GameSimulation
{
    protected readonly World World;
    protected readonly Group<float> Systems;
    private readonly FixedTimeStep _fixedTimeStep;
    private readonly IEntityFactory _entityFactory;

    protected GameSimulation()
    {
        World = World.Create(
            chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: SimulationConfig.ArchetypeCapacity,
            entityCapacity: SimulationConfig.EntityCapacity);
        
        Systems = new Group<float>(SimulationConfig.SimulationName);
        _fixedTimeStep = new FixedTimeStep(SimulationConfig.TickDelta);
        _entityFactory = new EntityFactory(World);
    }
    
    public uint CurrentTick { get; private set; }
    
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
        => _entityFactory.CreatePlayer(data);
    
    public Entity SpawnLocalPlayer(PlayerCharacter data) 
        => _entityFactory.CreateLocalPlayer(data);
    
    public Entity SpawnRemotePlayer(PlayerCharacter data)
        => _entityFactory.CreateRemotePlayer(data);

    public void DespawnEntity(Entity entity)
    {
        if (World.IsAlive(entity))
        {
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
}