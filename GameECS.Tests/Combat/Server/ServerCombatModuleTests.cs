using Arch.Core;
using GameECS.Modules.Combat.Server;
using GameECS.Modules.Combat.Shared.Data;
using GameECS.Server.Combat;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Combat.Data;
using GameECS.Shared.Entities.Data;
using Xunit;

namespace GameECS.Tests.Combat.Server;

public class ServerCombatModuleTests : IDisposable
{
    private readonly World _world;
    private readonly ServerCombatModule _module;

    public ServerCombatModuleTests()
    {
        _world = World.Create();
        _module = new ServerCombatModule(_world);
    }

    public void Dispose()
    {
        _module.Dispose();
        World.Destroy(_world);
    }

    [Theory]
    [InlineData(VocationType.Knight)]
    [InlineData(VocationType.Mage)]
    [InlineData(VocationType.Archer)]
    public void CreateCombatant_ShouldCreateEntityWithCorrectVocation(VocationType vocation)
    {
        // Act
        var entity = _module.CreateCombatant(vocation);

        // Assert
        Assert.True(_world.Has<PlayerVocation>(entity));
        Assert.True(_world.Has<Health>(entity));
        Assert.True(_world.Has<CombatStats>(entity));
        Assert.True(_world.Has<CanAttack>(entity));
        Assert.True(_world.Has<CombatEntity>(entity));

        var actualVocation = _world.Get<PlayerVocation>(entity);
        Assert.Equal(vocation, actualVocation.Type);
    }

    [Fact]
    public void CreateCombatant_Knight_ShouldHaveCorrectStats()
    {
        // Act
        var entity = _module.CreateCombatant(VocationType.Knight);

        // Assert
        var health = _world.Get<Health>(entity);
        var stats = _world.Get<CombatStats>(entity);

        Assert.Equal(Stats.Warrior.BaseHealth, health.Maximum);
        Assert.Equal(Stats.Warrior.BasePhysicalDamage, stats.PhysicalDamage);
        Assert.Equal(Stats.Warrior.BasePhysicalDefense, stats.PhysicalDefense);
        Assert.Equal(Stats.Warrior.BaseAttackRange, stats.AttackRange);
    }

    [Fact]
    public void CreateCombatant_Mage_ShouldHaveCorrectStats()
    {
        // Act
        var entity = _module.CreateCombatant(VocationType.Mage);

        // Assert
        var health = _world.Get<Health>(entity);
        var mana = _world.Get<Mana>(entity);
        var stats = _world.Get<CombatStats>(entity);

        Assert.Equal(Stats.Mage.BaseHealth, health.Maximum);
        Assert.Equal(Stats.Mage.BaseMana, mana.Maximum);
        Assert.Equal(Stats.Mage.BaseMagicDamage, stats.MagicDamage);
        Assert.Equal(Stats.Mage.BaseAttackRange, stats.AttackRange);
    }

    [Fact]
    public void CreateCombatant_Archer_ShouldHaveCorrectStats()
    {
        // Act
        var entity = _module.CreateCombatant(VocationType.Archer);

        // Assert
        var health = _world.Get<Health>(entity);
        var stats = _world.Get<CombatStats>(entity);

        Assert.Equal(Stats.Archer.BaseHealth, health.Maximum);
        Assert.Equal(Stats.Archer.BasePhysicalDamage, stats.PhysicalDamage);
        Assert.Equal(Stats.Archer.BaseAttackRange, stats.AttackRange);
        Assert.Equal(Stats.Archer.BaseAttackSpeed, stats.AttackSpeed);
    }

    [Fact]
    public void CreateCombatant_WithLevel_ShouldScaleStats()
    {
        // Arrange
        int level = 5;

        // Act
        var entity = _module.CreateCombatant(VocationType.Knight, level);

        // Assert
        var health = _world.Get<Health>(entity);
        var expectedHealth = Stats.Warrior.BaseHealth + (level - 1) * 10;
        Assert.Equal(expectedHealth, health.Maximum);
    }

    [Fact]
    public void RequestAttack_ShouldAddAttackRequestComponent()
    {
        // Arrange
        var attacker = _module.CreateCombatant(VocationType.Knight);
        var target = _module.CreateCombatant(VocationType.Mage);

        // Act
        _module.RequestAttack(attacker, target.Id, 0);

        // Assert
        Assert.True(_world.Has<AttackRequest>(attacker));
        var request = _world.Get<AttackRequest>(attacker);
        Assert.Equal(target.Id, request.TargetEntityId);
    }

    [Fact]
    public void RequestAttack_WhenDead_ShouldNotAddRequest()
    {
        // Arrange
        var attacker = _module.CreateCombatant(VocationType.Knight);
        var target = _module.CreateCombatant(VocationType.Mage);
        _world.Add<Dead>(attacker);

        // Act
        _module.RequestAttack(attacker, target.Id, 0);

        // Assert
        Assert.False(_world.Has<AttackRequest>(attacker));
    }

    [Fact]
    public void ApplyDirectDamage_ShouldReduceHealth()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);
        var initialHealth = _world.Get<Health>(entity).Current;

        // Act
        _module.ApplyDirectDamage(entity, 50, -1);

        // Assert
        var currentHealth = _world.Get<Health>(entity).Current;
        Assert.Equal(initialHealth - 50, currentHealth);
    }

    [Fact]
    public void ApplyDirectDamage_WhenKilling_ShouldMarkAsDead()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Mage);
        var maxHealth = _world.Get<Health>(entity).Maximum;

        // Act
        _module.ApplyDirectDamage(entity, maxHealth + 100, -1);

        // Assert
        Assert.True(_world.Has<Dead>(entity));
        Assert.False(_world.Has<CanAttack>(entity));
    }

    [Fact]
    public void ApplyDirectDamage_WhenInvulnerable_ShouldNotDamage()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);
        var initialHealth = _world.Get<Health>(entity).Current;
        _module.SetInvulnerable(entity, true);

        // Act
        _module.ApplyDirectDamage(entity, 50, -1);

        // Assert
        var currentHealth = _world.Get<Health>(entity).Current;
        Assert.Equal(initialHealth, currentHealth);
    }

    [Fact]
    public void HealEntity_ShouldRestoreHealth()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);
        _module.ApplyDirectDamage(entity, 50, -1);
        var healthAfterDamage = _world.Get<Health>(entity).Current;

        // Act
        var healed = _module.HealEntity(entity, 30);

        // Assert
        Assert.Equal(30, healed);
        var currentHealth = _world.Get<Health>(entity).Current;
        Assert.Equal(healthAfterDamage + 30, currentHealth);
    }

    [Fact]
    public void HealEntity_WhenDead_ShouldNotHeal()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);
        _world.Add<Dead>(entity);

        // Act
        var healed = _module.HealEntity(entity, 100);

        // Assert
        Assert.Equal(0, healed);
    }

    [Fact]
    public void HealEntity_ShouldNotExceedMaxHealth()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);
        _module.ApplyDirectDamage(entity, 10, -1);

        // Act
        var healed = _module.HealEntity(entity, 1000);

        // Assert
        Assert.Equal(10, healed);
        var health = _world.Get<Health>(entity);
        Assert.Equal(health.Maximum, health.Current);
    }

    [Fact]
    public void Resurrect_ShouldReviveDeadEntity()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);
        _module.ApplyDirectDamage(entity, 1000, -1);
        Assert.True(_world.Has<Dead>(entity));

        // Act
        _module.Resurrect(entity, 0.5f);

        // Assert
        Assert.False(_world.Has<Dead>(entity));
        Assert.True(_world.Has<CanAttack>(entity));
        
        var health = _world.Get<Health>(entity);
        Assert.True(health.Current > 0);
    }

    [Fact]
    public void SetInvulnerable_ShouldToggleInvulnerability()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);

        // Act & Assert - Set invulnerable
        _module.SetInvulnerable(entity, true);
        Assert.True(_world.Has<Invulnerable>(entity));

        // Act & Assert - Remove invulnerability
        _module.SetInvulnerable(entity, false);
        Assert.False(_world.Has<Invulnerable>(entity));
    }

    [Fact]
    public void IsAlive_ShouldReturnCorrectState()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);

        // Assert - Initially alive
        Assert.True(_module.IsAlive(entity));

        // Act - Kill entity
        _module.ApplyDirectDamage(entity, 1000, -1);

        // Assert - Now dead
        Assert.False(_module.IsAlive(entity));
    }

    [Fact]
    public void Tick_ShouldProcessAttackRequests()
    {
        // Arrange
        var attacker = _module.CreateCombatant(VocationType.Knight);
        var target = _module.CreateCombatant(VocationType.Mage);
        _module.RequestAttack(attacker, target.Id, 0);

        // Act
        _module.Tick(1);

        // Assert - Request should be processed and removed
        Assert.False(_world.Has<AttackRequest>(attacker));
    }

    [Fact]
    public void GetStatistics_ShouldReturnCombatStatistics()
    {
        // Arrange
        var entity = _module.CreateCombatant(VocationType.Knight);

        // Act
        var stats = _module.GetStatistics(entity);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.Value.TotalDamageDealt);
        Assert.Equal(0, stats.Value.TotalKills);
    }
}
