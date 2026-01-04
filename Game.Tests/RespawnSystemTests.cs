using System;
using FluentAssertions;
using Game.Domain.Entities;
using Game.DTOs.Player;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Services.Map;
using Game.ECS.Systems;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests;

public sealed class RespawnSystemTests
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
            systems.Add(new LifecycleSystem(world, NullLogger<LifecycleSystem>.Instance));
        }
    }

    [Fact]
    public void Dead_player_is_revived_and_teleported_to_spawn()
    {
        var map = new Map("test", width: 5, height: 5, layers: 1) { Id = 1 };
        var simulation = new TestSimulation(map);

        var snapshot = CreateSnapshot(playerId: 1, networkId: 1, x: 1, y: 1);
        var player = simulation.CreatePlayer(ref snapshot);

        // Move away from spawn and kill the player
        ref var position = ref simulation.World.Get<Position>(player);
        position.X = 3;
        position.Y = 3;

        ref var health = ref simulation.World.Get<Health>(player);
        ref var mana = ref simulation.World.Get<Mana>(player);
        health.Current = 0;
        mana.Current = 0;

        simulation.Update(SimulationConfig.TickDelta);

        simulation.World.Has<Dead>(player).Should().BeTrue();
        simulation.World.Has<Respawning>(player).Should().BeTrue();

        // Advance past respawn time (GameSimulation caps delta accumulation to 0.25s)
        var ticksToRespawn = (int)MathF.Ceiling(SimulationConfig.DefaultRespawnTime / SimulationConfig.TickDelta) + 1;
        for (var i = 0; i < ticksToRespawn; i++)
            simulation.Update(SimulationConfig.TickDelta);

        simulation.World.Has<Dead>(player).Should().BeFalse();
        simulation.World.Has<Respawning>(player).Should().BeFalse();

        ref var respawnedPosition = ref simulation.World.Get<Position>(player);
        respawnedPosition.Should().Be(new Position { X = 1, Y = 1, Z = 0 });

        health = ref simulation.World.Get<Health>(player);
        mana = ref simulation.World.Get<Mana>(player);

        health.Current.Should().Be((int)MathF.Ceiling(health.Max * SimulationConfig.ReviveHealthPercent));
        mana.Current.Should().Be((int)MathF.Ceiling(mana.Max * SimulationConfig.ReviveManaPercent));
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
