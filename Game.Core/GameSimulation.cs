using System.Numerics;
using Arch.Core;
using Arch.System;
using Game.ECS.Archetypes;
using Game.ECS.Components;
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
                new MovementSystem(_world),
                new HealthRegenerationSystem(_world),
                //new CombatSystem(_world),
                //new AISystem(_world),
                
                // 3. Physics/Collision
                //new CollisionSystem(_world),
                
                // 4. Cleanup
                //new DeathSystem(_world),
                
                // 5. Network (sempre por último)
                new NetworkSyncSystem(_world, 
                    serviceProvider.GetRequiredService<INetworkService>())
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

        public Entity SpawnPlayer(Guid playerId, uint networkId)
        {
            return _world.Create(GameArchetypes.PlayerCharacter, new object[]
            {
                new NetworkId { Value = networkId },
                new PlayerId { Value = playerId },
                new Position { Value = Vector3.Zero },
                new Rotation { Value = Quaternion.Identity },
                new Velocity { Value = Vector3.Zero },
                new Health { Current = 100, Max = 100, RegenerationRate = 5f },
                new Mana { Current = 100, Max = 100, RegenerationRate = 10f },
                new MovementSpeed { BaseSpeed = 5f, CurrentModifier = 1f },
                new AttackPower { Physical = 10, Magical = 5 },
                new Defense { Physical = 5, Magical = 5 },
                new CombatState(),
                new NetworkSync { Flags = SyncFlags.All },
                new PlayerInput(),
                new PlayerControlled()
            });
        }
    }
