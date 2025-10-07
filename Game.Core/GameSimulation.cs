using Arch.Core;
using Arch.System;
using Game.Abstractions;
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

        public Entity SpawnPlayer(int playerId, int networkId)
        {
            return _world.Create(GameArchetypes.PlayerCharacter, new object[]
            {
                new NetworkId { Value = networkId },
                new PlayerId { Value = playerId },
                new Position { Value = Coordinate.Zero },
                new Direction { Value = DirectionEnum.South.ToCoordinate() },
                new Velocity { Value = Vector2F.Zero },
                new Health { Current = 100, Max = 100, RegenerationRate = 5f },
                new Mana { Current = 100, Max = 100, RegenerationRate = 10f },
                new MovementSpeed { BaseSpeed = 5f, CurrentModifier = 1f },
                new AttackPower { Physical = 10, Magical = 5 },
                new Defense { Physical = 5, Magical = 5 },
                new CombatState(),
                new NetworkDirty { Flags = (ulong)SyncFlags.All },
                new PlayerInput(),
                new PlayerControlled()
            });
        }
    }
