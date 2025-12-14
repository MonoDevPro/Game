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

public sealed class AttackFacingTests
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
            systems.Add(new InputSystem(world));
            systems.Add(new CombatSystem(world, MapIndex, NullLogger<CombatSystem>.Instance));
            systems.Add(new DamageSystem(world, NullLogger<DamageSystem>.Instance));
        }
    }

    [Fact]
    public void Player_can_damage_target_when_attacking_while_standing_still_and_facing_it()
    {
        var map = new Map("test", width: 5, height: 5, layers: 1) { Id = 1 };
        var simulation = new TestSimulation(map);

        var attackerSnapshot = CreateSnapshot(playerId: 1, networkId: 1, x: 1, y: 1, dirX: 1, dirY: 0);
        var targetSnapshot = CreateSnapshot(playerId: 2, networkId: 2, x: 2, y: 1, dirX: -1, dirY: 0);

        var attacker = simulation.CreatePlayer(ref attackerSnapshot);
        var target = simulation.CreatePlayer(ref targetSnapshot);

        // Press attack without movement.
        ref var input = ref simulation.World.Get<Input>(attacker);
        input.InputX = 0;
        input.InputY = 0;
        input.Flags = InputFlags.BasicAttack;

        // Sanity: still facing east.
        simulation.World.Get<Direction>(attacker).Should().Be(new Direction { X = 1, Y = 0 });

        int initialHp = simulation.World.Get<Health>(target).Current;

        // Run enough ticks to pass ConjureDuration (1s) + process deferred damage.
        for (int i = 0; i < SimulationConfig.TicksPerSecond + 2; i++)
            simulation.Update(SimulationConfig.TickDelta);

        simulation.World.Get<Direction>(attacker).Should().Be(new Direction { X = 1, Y = 0 });
        simulation.World.Get<Health>(target).Current.Should().BeLessThan(initialHp);
    }

    private static PlayerData CreateSnapshot(int playerId, int networkId, int x, int y, sbyte dirX, sbyte dirY)
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
            DirX: dirX,
            DirY: dirY,
            Hp: 100,
            MaxHp: 100,
            HpRegen: 0,
            Mp: 100,
            MaxMp: 100,
            MpRegen: 0,
            MovementSpeed: 1,
            AttackSpeed: 2,
            PhysicalAttack: 10,
            MagicAttack: 10,
            PhysicalDefense: 5,
            MagicDefense: 5);
    }
}
