using Arch.Core;
using Arch.System;
using Game.Abstractions;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.ECS.Archetypes;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Server.Players;

namespace Game.Server.Simulation;

public readonly record struct PlayerVitals(int CurrentHp, int MaxHp, int CurrentMp, int MaxMp, float HpRegenRate, float MpRegenRate);

public class GameSimulation
{
    private readonly World _world;
    private readonly Group<float> _systems;
    private readonly FixedTimeStep _fixedTimeStep;

    public const float FixedDeltaTime = 1f / 60f; // 60 ticks/segundo
    public uint CurrentTick { get; private set; }

    public GameSimulation(IServiceProvider serviceProvider)
    {
        _world = World.Create();
        _fixedTimeStep = new FixedTimeStep(FixedDeltaTime);

        // Ordem de execução dos sistemas é importante!
        _systems = new Group<float>("Simulation", new ISystem<float>[]
        {
            // 1. Input
            new PlayerInputSystem(_world),

            // 2. Gameplay
            new MovementSystem(_world, serviceProvider.GetRequiredService<MapService>()),
            new HealthRegenerationSystem(_world),
            
            // 3. Sincronização de rede
            new PlayerSyncBroadcasterSystem(_world, serviceProvider.GetRequiredService<INetworkManager>())
        });
    }

    public void Update(float deltaTime)
    {
        _fixedTimeStep.Accumulate(deltaTime);

        while (_fixedTimeStep.ShouldUpdate())
        {
            CurrentTick++;

            _systems.BeforeUpdate(FixedDeltaTime);

            _systems.Update(FixedDeltaTime);

            _systems.AfterUpdate(FixedDeltaTime);

            _fixedTimeStep.Step();
        }
    }

    public Entity SpawnPlayer(int playerId, int networkId, Coordinate spawnPosition, DirectionEnum facing, Stats stats)
    {
        var maxHp = Math.Max(1, stats.MaxHp);
        var currentHp = Math.Clamp(stats.CurrentHp, 0, maxHp);
        var hpRegen = Math.Max(0f, stats.HpRegenPerTick());

        var maxMp = Math.Max(0, stats.MaxMp);
        var currentMp = Math.Clamp(stats.CurrentMp, 0, maxMp);
        var mpRegen = Math.Max(0f, stats.MpRegenPerTick());

        var movementModifier = (float)Math.Max(0.1, stats.MovementSpeed);

        var attackPower = new AttackPower { Physical = stats.PhysicalAttack, Magical = stats.MagicAttack };
        var defense = new Defense { Physical = stats.PhysicalDefense, Magical = stats.MagicDefense };

        var entity = _world.Create(GameArchetypes.PlayerCharacter);
        var components = new object[]
        {
            new NetworkId { Value = networkId },
            new PlayerId { Value = playerId },
            new Position { Value = new FCoordinate(spawnPosition.X, spawnPosition.Y) },
            new GridPosition { Value = spawnPosition },
            new Direction { Value = facing.ToCoordinate() },
            new Velocity { Value = FCoordinate.Zero },
            new Health { Current = currentHp, Max = maxHp, RegenerationRate = hpRegen },
            new Mana { Current = currentMp, Max = maxMp, RegenerationRate = mpRegen },
            new MovementSpeed { BaseSpeed = 5f, CurrentModifier = movementModifier },
            attackPower,
            defense,
            new CombatState(),
            new NetworkDirty { Flags = (ulong)SyncFlags.InitialSync },
            new PlayerInput(),
            new PlayerControlled()
        };
        _world.SetRange(entity, components);
        return entity;
    }

    public void DespawnEntity(Entity entity)
    {
        if (_world.IsAlive(entity))
        {
            _world.Destroy(entity);
        }
    }

    public bool TryGetPlayerState(Entity entity, 
        out Coordinate position, out DirectionEnum facing, out float speed)
    {
        position = Coordinate.Zero;
        facing = DirectionEnum.South;
        speed = 0f;

        if (!_world.TryGet(entity, out GridPosition posComponent))
        {
            return false;
        }

        position = posComponent.Value;

        if (_world.TryGet(entity, out Direction dirComponent))
        {
            facing = dirComponent.Value.ToDirectionEnum();
        }

        if (_world.TryGet(entity, out MovementSpeed speedComponent))
        {
            speed = speedComponent.BaseSpeed * speedComponent.CurrentModifier;
        }

        return true;
    }

    public bool TryGetPlayerVitals(Entity entity, out PlayerVitals vitals)
    {
        vitals = default;

        if (!_world.TryGet(entity, out Health health) || !_world.TryGet(entity, out Mana mana))
        {
            return false;
        }

        vitals = new PlayerVitals(health.Current, health.Max, mana.Current, mana.Max, health.RegenerationRate, mana.RegenerationRate);
        return true;
    }

    public bool TryApplyPlayerInput(Entity entity, GridOffset movement, GridOffset mouseLook, ushort buttons)
    {
        if (!_world.Has<PlayerInput>(entity))
            _world.Add(entity, new PlayerInput());

        // Clamp para segurança (sbyte já é -128 a 127, mas garantimos -1,0,1)
        movement = new GridOffset(
            movement.X < -1 ? (sbyte)-1 : (movement.X > 1 ? (sbyte)1 : movement.X),
            movement.Y < -1 ? (sbyte)-1 : (movement.Y > 1 ? (sbyte)1 : movement.Y)
        );

        ref var input = ref _world.Get<PlayerInput>(entity);
        input.Movement = movement;
        input.MouseLook = mouseLook;
        input.Flags = (InputFlags)buttons;

        return true;
    }
}