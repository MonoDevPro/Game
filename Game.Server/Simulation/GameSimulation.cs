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

namespace Game.Core;

public readonly record struct PlayerNetworkStateData(int NetworkId, Coordinate Position, Coordinate Facing);
public readonly record struct PlayerVitals(int CurrentHp, int MaxHp, int CurrentMp, int MaxMp, float HpRegenRate, float MpRegenRate);

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

        return _world.Create(GameArchetypes.PlayerCharacter, new object[]
        {
            new NetworkId { Value = networkId },
            new PlayerId { Value = playerId },
            new Position { Value = spawnPosition },
            new Direction { Value = facing.ToCoordinate() },
            new Velocity { Value = FCoordinate.Zero },
            new MoveAccumulator { Value = FCoordinate.Zero },
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

    public bool TryApplyPlayerInput(Entity entity, sbyte moveX, sbyte moveY, ushort buttons)
    {
        if (!_world.Has<PlayerInput>(entity))
        {
            _world.Add(entity, new PlayerInput());
        }

        // Clamp para segurança (sbyte já é -128 a 127, mas garantimos -1,0,1)
        int x = Math.Clamp((int)moveX, -1, 1);
        int y = Math.Clamp((int)moveY, -1, 1);

        ref var input = ref _world.Get<PlayerInput>(entity);
        input.Movement = new Coordinate(x, y);
        input.Flags = (InputFlags)buttons;

        return true;
    }

    /// <summary>
    /// Decodifica um eixo de input de byte (0,1,2) para int (-1,0,1)
    /// </summary>
    private static int DecodeInputAxis(byte encoded)
    {
        return encoded switch
        {
            0 => -1,  // Esquerda ou Cima
            1 => 0,   // Sem movimento
            2 => 1,   // Direita ou Baixo
            _ => 0    // Valor inválido = sem movimento
        };
    }

    public void CollectDirtyPlayers(List<PlayerNetworkStateData> buffer)
    {
        _world.Query(in _dirtyPlayersQuery, (Entity entity, ref NetworkId netId, ref Position position, ref Direction direction, ref NetworkDirty dirty) =>
        {
            buffer.Add(new PlayerNetworkStateData(netId.Value, position.Value, direction.Value));
            _world.ClearNetworkDirty(entity, SyncFlags.Movement);
        });
    }
}