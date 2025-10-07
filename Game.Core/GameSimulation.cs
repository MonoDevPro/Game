using System;
using System.Collections.Generic;
using Arch.Core;
using Arch.Core.Extensions;
using Arch.System;
using Game.Abstractions;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.ECS.Archetypes;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;

namespace Game.Core;

public class GameSimulation
{
    private readonly World _world;
    private readonly Group<float> _systems;
    private readonly FixedTimeStep _fixedTimeStep;
    private readonly QueryDescription _dirtyPlayersQuery = new QueryDescription().WithAll<NetworkId, Position, Direction, NetworkDirty>();

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
            //new CombatSystem(_world),
            //new AISystem(_world),

            // 3. Physics/Collision
            //new CollisionSystem(_world),

            // 4. Cleanup
            //new DeathSystem(_world),

            // 5. Network (sempre por último)
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

    public Entity SpawnPlayer(int playerId, int networkId, Coordinate spawnPosition, DirectionEnum facing, Stats? stats = null)
    {
        var maxHp = 100;
        var currentHp = maxHp;
        var hpRegen = 5f;

        var maxMp = 100;
        var currentMp = maxMp;
        var mpRegen = 10f;

        var movementModifier = 1f;

        var attackPower = new AttackPower { Physical = 10, Magical = 5 };
        var defense = new Defense { Physical = 5, Magical = 5 };

        if (stats is not null)
        {
            maxHp = Math.Max(1, stats.MaxHp);
            currentHp = Math.Clamp(stats.CurrentHp, 0, maxHp);
            hpRegen = Math.Max(0f, stats.HpRegenPerTick());

            maxMp = Math.Max(0, stats.MaxMp);
            currentMp = Math.Clamp(stats.CurrentMp, 0, maxMp);
            mpRegen = Math.Max(0f, stats.MpRegenPerTick());

            movementModifier = (float)Math.Max(0.1, stats.MovementSpeed);

            attackPower = new AttackPower { Physical = stats.PhysicalAttack, Magical = stats.MagicAttack };
            defense = new Defense { Physical = stats.PhysicalDefense, Magical = stats.MagicDefense };
        }

        return _world.Create(GameArchetypes.PlayerCharacter, new object[]
        {
            new NetworkId { Value = networkId },
            new PlayerId { Value = playerId },
            new Position { Value = spawnPosition },
            new Direction { Value = facing.ToCoordinate() },
            new Velocity { Value = Vector2F.Zero },
            new MoveAccumulator { Value = Vector2F.Zero },
            new Health { Current = currentHp, Max = maxHp, RegenerationRate = hpRegen },
            new Mana { Current = currentMp, Max = maxMp, RegenerationRate = mpRegen },
            new MovementSpeed { BaseSpeed = 5f, CurrentModifier = movementModifier },
            attackPower,
            defense,
            new CombatState(),
            new NetworkDirty { Flags = (ulong)SyncFlags.All },
            new PlayerInput(),
            new PlayerControlled()
        });
    }

    public void DespawnEntity(Entity entity)
    {
        if (_world.IsAlive(entity))
        {
            _world.Destroy(entity);
        }
    }

    public bool TryGetPlayerState(Entity entity, out Coordinate position, out DirectionEnum facing)
    {
        position = Coordinate.Zero;
        facing = DirectionEnum.South;

        if (!_world.TryGet(entity, out Position posComponent))
        {
            return false;
        }

        position = posComponent.Value;

        if (_world.TryGet(entity, out Direction dirComponent))
        {
            facing = dirComponent.Value.ToDirectionEnum();
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

    public bool TryApplyPlayerInput(Entity entity, sbyte moveX, sbyte moveY, ushort buttons, uint sequence)
    {
        if (!_world.Has<PlayerInput>(entity))
        {
            _world.Add(entity, new PlayerInput());
        }

        ref var input = ref _world.Get<PlayerInput>(entity);
        input.SequenceNumber = sequence;
        input.Movement = new Coordinate(Math.Clamp((int)moveX, -1, 1), Math.Clamp((int)moveY, -1, 1));
        input.Flags = (InputFlags)buttons;

        return true;
    }

    public void CollectDirtyPlayers(List<PlayerNetworkStateData> buffer)
    {
        _world.Query(in _dirtyPlayersQuery, (Entity entity, ref NetworkId netId, ref Position position, ref Direction direction, ref NetworkDirty dirty) =>
        {
            buffer.Add(new PlayerNetworkStateData(netId.Value, position.Value, direction.Value.ToDirectionEnum(), CurrentTick));
            _world.ClearNetworkDirty(entity, SyncFlags.Movement);
        });
    }
}

public readonly record struct PlayerVitals(int CurrentHp, int MaxHp, int CurrentMp, int MaxMp, float HpRegenRate, float MpRegenRate);
