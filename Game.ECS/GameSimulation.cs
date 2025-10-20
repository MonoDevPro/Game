using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.ECS.Archetypes;
using Game.ECS.Components;
using Game.ECS.DTOs;
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
    protected readonly World World = World.Create();
    protected readonly Group<float> Systems = new Group<float>("Simulation");
    private readonly FixedTimeStep _fixedTimeStep = new FixedTimeStep(SimulationConfig.TickDelta);
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

    public Entity SpawnPlayer(PlayerSpawnData data)
    {
        var entity = World.Create(GameArchetypes.PlayerCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new PlayerId { Value = data.PlayerId },
            new Position { X = data.SpawnX, Y = data.SpawnY, Z = data.SpawnZ },
            new Facing { DirectionX = data.FacingX, DirectionY = data.FacingY },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Mana { Current = data.Mp, Max = data.MaxMp, RegenerationRate = data.MpRegen },
            new Walkable { BaseSpeed = 5f, CurrentModifier = data.MovementSpeed },
            new Attackable { BaseSpeed = 1f, CurrentModifier = data.AttackSpeed },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { },
            new NetworkDirty { Flags = SyncFlags.All },
            new PlayerInput(),
            new PlayerControlled()
        };
        World.SetRange(entity, components);
        return entity;
    }

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