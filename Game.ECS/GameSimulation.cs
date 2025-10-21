using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Data;
using Game.ECS.Entities.Factories;
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
public abstract class GameSimulation : GameSystem
{
    protected readonly Group<float> Systems;
    
    private readonly FixedTimeStep _fixedTimeStep;
    
    /// <summary>
    /// Tick atual da simulação. Incrementa a cada atualização.
    /// </summary>
    public uint CurrentTick { get; private set; }
    
    protected GameSimulation() : this(World.Create(
        chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
        minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
        archetypeCapacity: SimulationConfig.ArchetypeCapacity,
        entityCapacity: SimulationConfig.EntityCapacity), new GameEventSystem()) { }

    private GameSimulation(World world, GameEventSystem gameEvents) : base(world, gameEvents, new EntityFactory(world, gameEvents))
    {
        Systems = new Group<float>(SimulationConfig.SimulationName);
        _fixedTimeStep = new FixedTimeStep(SimulationConfig.TickDelta);
    }

    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    protected abstract void ConfigureSystems(World world, GameEventSystem gameEvents, EntityFactory factory, Group<float> systems);

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
}