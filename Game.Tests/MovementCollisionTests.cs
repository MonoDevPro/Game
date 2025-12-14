using FluentAssertions;
using Game.Domain.Entities;
using Game.DTOs.Game.Player;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Services.Map;
using Game.ECS.Systems;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests;

public sealed class MovementCollisionTests
{
    private sealed class TestSimulation : GameSimulation
    {
        public TestSimulation(Map map)
            : base(NullLogger<GameSimulation>.Instance)
        {
            RegisterMap(map.Id, new MapGrid(map.Width, map.Height, map.Layers, map.GetCollisionGrid()), new MapSpatial());
            ConfigureSystems(World, Systems);
            Systems.Initialize();
        }

        protected override void ConfigureSystems(Arch.Core.World world, Arch.System.Group<float> systems)
        {
            systems.Add(new SpatialSyncSystem(world, MapIndex, NullLogger<SpatialSyncSystem>.Instance));
            systems.Add(new MovementSystem(world, MapIndex, EventBus, NullLogger<MovementSystem>.Instance));
        }
    }

    [Fact]
    public void Movement_is_blocked_when_target_tile_is_occupied()
    {
        var map = new Map("test", width: 5, height: 5, layers: 1) { Id = 1 };
        var simulation = new TestSimulation(map);

        var player1Snapshot = CreateSnapshot(playerId: 1, networkId: 1, x: 1, y: 1);
        var player2Snapshot = CreateSnapshot(playerId: 2, networkId: 2, x: 2, y: 1);

        var player1 = simulation.CreatePlayer(ref player1Snapshot);
        simulation.CreatePlayer(ref player2Snapshot);

        simulation.World.Add(player1, new MovementIntent
        {
            TargetPosition = new Position { X = 2, Y = 1, Z = 0 }
        });

        simulation.Update(SimulationConfig.TickDelta);

        simulation.World.TryGet(player1, out MovementBlocked blocked).Should().BeTrue();
        blocked.Reason.Should().Be(MovementResult.BlockedByEntity);

        ref var position = ref simulation.World.Get<Position>(player1);
        position.Should().Be(new Position { X = 1, Y = 1, Z = 0 });
        simulation.World.Has<MovementApproved>(player1).Should().BeFalse();
        simulation.World.Has<MovementIntent>(player1).Should().BeFalse();
    }

    private static PlayerData CreateSnapshot(int playerId, int networkId, int x, int y)
    {
        return new PlayerData(
            PlayerId: playerId,
            NetworkId: networkId,
            MapId: 1,
            Name: $"Player{playerId}",
            Gender: 0,
            Vocation: 0,
            X: x,
            Y: y,
            Z: 0,
            DirX: 1,
            DirY: 0,
            Hp: 100,
            MaxHp: 100,
            HpRegen: 0,
            Mp: 100,
            MaxMp: 100,
            MpRegen: 0,
            MovementSpeed: 1,
            AttackSpeed: 1,
            PhysicalAttack: 10,
            MagicAttack: 10,
            PhysicalDefense: 5,
            MagicDefense: 5);
    }
}
