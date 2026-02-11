using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using FluentAssertions;
using Game.Contracts;
using Game.Infrastructure.ArchECS;
using Game.Infrastructure.ArchECS.Services.Combat;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.EntityRegistry;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;
using Xunit;

namespace Game.Tests;

[Collection(nameof(EcsCollection))]
public class CombatProjectileSystemTests
{
    [Fact]
    public void ProjectileHit_ShouldApplyDamage_AndEmitHitEventWithCharacterIds()
    {
        using var world = World.Create();
        var map = new WorldMap(1, "Test", 30, 30, flags: MapFlags.PvPEnabled);
        using var combat = new CombatModule(world, map, BuildProjectileConfig(range: 4, speed: SimulationConfig.TicksPerSecond));
        var registry = world.GetEntityRegistry();

        var attacker = CreatePlayer(world, map, characterId: 101, x: 5, y: 5);
        var target = CreatePlayer(world, map, characterId: 202, x: 7, y: 5);
        combat.AddCombatComponents(attacker, BuildStats(), vocation: 1, teamId: 0);
        combat.AddCombatComponents(target, BuildStats(), vocation: 1, teamId: 0);
        registry.Register(101, attacker, EntityDomain.Combat);
        registry.Register(202, target, EntityDomain.Combat);

        var attackerEntity = registry.GetEntity(101, EntityDomain.Combat);
        combat.RequestBasicAttack(attackerEntity, dirX: 1, dirY: 0, serverTick: 0).Should().BeTrue();
        Tick(combat, from: 0, to: 6);

        var targetEntity = registry.GetEntity(202, EntityDomain.Combat);
        world.Get<CombatStats>(targetEntity).CurrentHealth.Should().Be(90);

        combat.TryDrainEvents(out var events).Should().BeTrue();
        events.Any(e =>
            e.Type == CombatEventType.Hit &&
            e.AttackerId == 101 &&
            e.TargetId == 202 &&
            e.Damage == 10).Should().BeTrue();
    }

    [Fact]
    public void ProjectileHit_OnLastRangeCell_ShouldStillApplyDamage()
    {
        using var world = World.Create();
        var map = new WorldMap(1, "Test", 30, 30, flags: MapFlags.PvPEnabled);
        using var combat = new CombatModule(world, map, BuildProjectileConfig(range: 3, speed: SimulationConfig.TicksPerSecond));
        var registry = world.GetEntityRegistry();

        var attacker = CreatePlayer(world, map, characterId: 301, x: 5, y: 5);
        var target = CreatePlayer(world, map, characterId: 302, x: 8, y: 5); // exactly 3 cells away
        combat.AddCombatComponents(attacker, BuildStats(), vocation: 1, teamId: 0);
        combat.AddCombatComponents(target, BuildStats(), vocation: 1, teamId: 0);
        registry.Register(301, attacker, EntityDomain.Combat);
        registry.Register(302, target, EntityDomain.Combat);

        var attackerEntity = registry.GetEntity(301, EntityDomain.Combat);
        combat.RequestBasicAttack(attackerEntity, dirX: 1, dirY: 0, serverTick: 0).Should().BeTrue();
        Tick(combat, from: 0, to: 6);

        var targetEntity = registry.GetEntity(302, EntityDomain.Combat);
        world.Get<CombatStats>(targetEntity).CurrentHealth.Should().Be(90);
    }

    private static Entity CreatePlayer(World world, WorldMap map, int characterId, int x, int y)
    {
        var entity = world.Create(
            new CharacterId { Value = characterId },
            new Position { X = x, Y = y },
            new FloorId { Value = 0 });

        map.AddEntity(new Position { X = x, Y = y }, floor: 0, entity).Should().BeTrue();

        return entity;
    }

    [Fact]
    public void ProjectileDamage_ShouldHappenOnlyAfterProjectileTravel()
    {
        using var world = World.Create();
        var map = new WorldMap(1, "Test", 30, 30, flags: MapFlags.PvPEnabled);
        using var combat = new CombatModule(world, map, BuildProjectileConfig(range: 6, speed: 12f));
        var registry = world.GetEntityRegistry();

        var attacker = CreatePlayer(world, map, characterId: 401, x: 5, y: 5);
        var target = CreatePlayer(world, map, characterId: 402, x: 7, y: 5);
        combat.AddCombatComponents(attacker, BuildStats(), vocation: 1, teamId: 0);
        combat.AddCombatComponents(target, BuildStats(), vocation: 1, teamId: 0);
        registry.Register(401, attacker, EntityDomain.Combat);
        registry.Register(402, target, EntityDomain.Combat);

        var attackerEntity = registry.GetEntity(401, EntityDomain.Combat);
        var targetEntity = registry.GetEntity(402, EntityDomain.Combat);

        combat.RequestBasicAttack(attackerEntity, dirX: 1, dirY: 0, serverTick: 0).Should().BeTrue();

        Tick(combat, from: 0, to: 8);
        world.Get<CombatStats>(targetEntity).CurrentHealth.Should().Be(100);

        Tick(combat, from: 9, to: 9);
        world.Get<CombatStats>(targetEntity).CurrentHealth.Should().Be(90);
    }

    private static CombatConfig BuildProjectileConfig(int range, float speed)
    {
        return new CombatConfig
        {
            Vocations = new Dictionary<byte, CombatConfig.VocationConfig>
            {
                [1] = new CombatConfig.VocationConfig
                {
                    BaseCooldownMs = 1,
                    ManaCost = 0,
                    Range = range,
                    ProjectileSpeed = speed,
                    DamageBase = 10,
                    DamageScale = 0f,
                    DamageStat = CombatDamageStat.Intelligence
                }
            }
        };
    }

    private static CombatStats BuildStats()
    {
        return new CombatStats
        {
            Strength = 10,
            Endurance = 10,
            Agility = 10,
            Intelligence = 20,
            Willpower = 10,
            MaxHealth = 100,
            CurrentHealth = 100,
            MaxMana = 100,
            CurrentMana = 100
        };
    }

    private static void Tick(CombatModule combat, long from, long to)
    {
        for (var tick = from; tick <= to; tick++)
        {
            combat.Tick(tick);
        }
    }
}
