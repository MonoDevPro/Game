using System;
using Arch.Core;
using Arch.System;
using Game.Domain.Entities;
using Game.ECS.Archetypes;
using Game.ECS.Components;
using Game.ECS.Services;
using Game.ECS.Utils;
using GodotClient.Scenes.Game;
using GodotClient.Simulation.Systems;

namespace GodotClient.Simulation;


public sealed class ClientSimulation
{
    private readonly World _world;
    private readonly Group<float> _systems;
    private readonly FixedTimeStep _fixedTimeStep;

    public const float FixedDeltaTime = 1f / 60f; // 60 ticks/segundo
    public uint CurrentTick { get; private set; }
    
    private readonly GameScript _game;

    public ClientSimulation(GameScript game, MapService mapService)
    {
        _world = World.Create();
        _fixedTimeStep = new FixedTimeStep(FixedDeltaTime);
        _game = game;

        // Ordem de execução dos sistemas é importante!
        _systems = new Group<float>("Simulation", new ISystem<float>[]
        {
            // 1. Input
            new ClientInputSystem(_world),

        });
    }

    public void Update(float deltaTime)
    {
        if (!_game.CanSendInput)
            return;
        
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

    public Entity SpawnPlayer(int playerId, int networkId, 
        int spawnX, int spawnY, int spawnZ, int facingX, int facingY, Stats stats)
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
            new Position { X = spawnX, Y = spawnY, Z = spawnZ },
            new Facing { DirectionX = facingX, DirectionY = facingY },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = currentHp, Max = maxHp, RegenerationRate = hpRegen },
            new Mana { Current = currentMp, Max = maxMp, RegenerationRate = mpRegen },
            new Walkable { BaseSpeed = 5f, CurrentModifier = movementModifier },
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
    
    public bool ApplyMovementFromServer(Entity entity, int posX, int posY, int posZ, 
        int facingX, int facingY, float speed)
    {
        bool updated = false;

        ref var position = ref _world.Get<Position>(entity);
        if (position.X != posX || position.Y != posY || position.Z != posZ)
        {
            position.X = posX;
            position.Y = posY;
            position.Z = posZ;
            updated = true;
        }

        ref var facing = ref _world.Get<Facing>(entity);
        if (facing.DirectionX != facingX || facing.DirectionY != facingY)
        {
            facing.DirectionX = facingX;
            facing.DirectionY = facingY;
            updated = true;
        }

        ref var velocity = ref _world.Get<Velocity>(entity);
        if (Math.Abs(velocity.Speed - speed) > 0.01f)
        {
            velocity.Speed = speed;
            updated = true;
        }

        return updated;
    }
}