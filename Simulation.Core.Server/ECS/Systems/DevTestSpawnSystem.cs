using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Components.Data;
using Simulation.Core.ECS.Resource;

namespace Simulation.Core.Server.ECS.Systems;

public sealed class DevTestSpawnSystem(World world, PlayerFactoryResource playerFactory, ILogger<DevTestSpawnSystem> logger)
    : BaseSystem<World, float>(world)
{
    private bool _spawned;
    private float _lastLogTime;
    private Entity _playerEntity;

    public override void Update(in float dt)
    {
        _lastLogTime += dt;
        if (_lastLogTime >= 5f)
        {
            _lastLogTime = 0f;
            
            /*if (World.IsAlive(_playerEntity) && !World.Has<MoveIntent, MoveTimer>(_playerEntity))
            {
                var moveIntent = new MoveIntent(new Direction(1, 1));
                World.Add(_playerEntity, moveIntent);
                
                var position = World.Get<Position>(_playerEntity);
                var direction = World.Get<Direction>(_playerEntity);
            
            }*/
            
            var position = World.Get<Position>(_playerEntity);
            var direction = World.Get<Direction>(_playerEntity);
            logger.LogInformation("DevTestSpawnSystem: Player Position - X: {PosX}, Y: {PosY}", position.X, position.Y);
            logger.LogInformation("DevTestSpawnSystem: Player Direction - X: {DirX}, Y: {DirY}", direction.X, direction.Y);
            
        }
        
        if (_spawned) return;

        var player = new PlayerData
        {
            Id = 999,
            Name = "TestPlayer",
            Gender = Gender.Male,
            Vocation = Vocation.Mage,
            PosX = 10,
            PosY = 10,
            DirX = 1,
            DirY = 0,
            HealthCurrent = 100,
            HealthMax = 100,
            MoveSpeed = 5f,
            AttackCastTime = 0.2f,
            AttackCooldown = 0.8f,
            AttackDamage = 10,
            AttackRange = 1
        };

        playerFactory.TryCreatePlayer(player, out _playerEntity);
        _spawned = true;
    }
}