using GameECS.Modules.Combat.Shared.Data;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Combat.Data;
using GameECS.Shared.Entities.Data;
using Xunit;

namespace GameECS.Tests.Combat.Components;

public class CombatComponentsTests
{
    #region Health Tests

    [Fact]
    public void Health_TakeDamage_ShouldReduceCurrentHealth()
    {
        // Arrange
        var health = new Health(100);

        // Act
        var actualDamage = health.TakeDamage(30);

        // Assert
        Assert.Equal(30, actualDamage);
        Assert.Equal(70, health.Current);
    }

    [Fact]
    public void Health_TakeDamage_ShouldNotGoBelowZero()
    {
        // Arrange
        var health = new Health(50);

        // Act
        var actualDamage = health.TakeDamage(100);

        // Assert
        Assert.Equal(50, actualDamage);
        Assert.Equal(0, health.Current);
    }

    [Fact]
    public void Health_IsDead_ShouldReturnTrueWhenZero()
    {
        // Arrange
        var health = new Health(100);
        health.TakeDamage(100);

        // Assert
        Assert.True(health.IsDead);
    }

    [Fact]
    public void Health_Heal_ShouldRestoreHealth()
    {
        // Arrange
        var health = new Health(100);
        health.TakeDamage(50);

        // Act
        var actualHeal = health.Heal(30);

        // Assert
        Assert.Equal(30, actualHeal);
        Assert.Equal(80, health.Current);
    }

    [Fact]
    public void Health_Heal_ShouldNotExceedMaximum()
    {
        // Arrange
        var health = new Health(100);
        health.TakeDamage(10);

        // Act
        var actualHeal = health.Heal(50);

        // Assert
        Assert.Equal(10, actualHeal);
        Assert.Equal(100, health.Current);
    }

    [Fact]
    public void Health_Percentage_ShouldReturnCorrectValue()
    {
        // Arrange
        var health = new Health(100);
        health.TakeDamage(25);

        // Assert
        Assert.Equal(0.75f, health.Percentage);
    }

    [Fact]
    public void Health_Reset_ShouldRestoreToMaximum()
    {
        // Arrange
        var health = new Health(100);
        health.TakeDamage(80);

        // Act
        health.Reset();

        // Assert
        Assert.Equal(100, health.Current);
        Assert.True(health.IsFullHealth);
    }

    #endregion

    #region Mana Tests

    [Fact]
    public void Mana_TryConsume_ShouldReduceMana()
    {
        // Arrange
        var mana = new Mana(100);

        // Act
        var result = mana.TryConsume(30);

        // Assert
        Assert.True(result);
        Assert.Equal(70, mana.Current);
    }

    [Fact]
    public void Mana_TryConsume_ShouldFailWhenInsufficient()
    {
        // Arrange
        var mana = new Mana(20);

        // Act
        var result = mana.TryConsume(50);

        // Assert
        Assert.False(result);
        Assert.Equal(20, mana.Current);
    }

    [Fact]
    public void Mana_Regenerate_ShouldIncreaseMana()
    {
        // Arrange
        var mana = new Mana(100, regenPerTick: 5);
        mana.TryConsume(50);

        // Act
        mana.Regenerate();

        // Assert
        Assert.Equal(55, mana.Current);
    }

    [Fact]
    public void Mana_Regenerate_ShouldNotExceedMaximum()
    {
        // Arrange
        var mana = new Mana(100, regenPerTick: 10);
        mana.TryConsume(5);

        // Act
        mana.Regenerate();

        // Assert
        Assert.Equal(100, mana.Current);
        Assert.True(mana.IsFull);
    }

    #endregion

    #region AttackCooldown Tests

    [Fact]
    public void AttackCooldown_IsReady_ShouldReturnTrueWhenCooldownComplete()
    {
        // Arrange
        var cooldown = new AttackCooldown();
        cooldown.TriggerCooldown(0, 10);

        // Assert
        Assert.False(cooldown.IsReady(5));
        Assert.True(cooldown.IsReady(10));
        Assert.True(cooldown.IsReady(15));
    }

    [Fact]
    public void AttackCooldown_GetRemainingTicks_ShouldReturnCorrectValue()
    {
        // Arrange
        var cooldown = new AttackCooldown();
        cooldown.TriggerCooldown(0, 10);

        // Assert
        Assert.Equal(5, cooldown.GetRemainingTicks(5));
        Assert.Equal(0, cooldown.GetRemainingTicks(10));
        Assert.Equal(0, cooldown.GetRemainingTicks(15));
    }

    #endregion

    #region CombatStats Tests

    [Fact]
    public void CombatStats_FromVocation_Knight_ShouldHaveCorrectValues()
    {
        // Act
        var stats = CombatStats.FromVocation(VocationType.Knight);

        // Assert
        Assert.Equal(Stats.Warrior.BasePhysicalDamage, stats.PhysicalDamage);
        Assert.Equal(Stats.Warrior.BaseMagicDamage, stats.MagicDamage);
        Assert.Equal(Stats.Warrior.BasePhysicalDefense, stats.PhysicalDefense);
        Assert.Equal(Stats.Warrior.BaseAttackRange, stats.AttackRange);
    }

    [Fact]
    public void CombatStats_FromVocation_Mage_ShouldHaveCorrectValues()
    {
        // Act
        var stats = CombatStats.FromVocation(VocationType.Mage);

        // Assert
        Assert.Equal(Stats.Mage.BaseMagicDamage, stats.MagicDamage);
        Assert.Equal(Stats.Mage.BaseMagicDefense, stats.MagicDefense);
        Assert.Equal(Stats.Mage.BaseAttackRange, stats.AttackRange);
    }

    [Fact]
    public void CombatStats_FromVocation_Archer_ShouldHaveCorrectValues()
    {
        // Act
        var stats = CombatStats.FromVocation(VocationType.Archer);

        // Assert
        Assert.Equal(Stats.Archer.BasePhysicalDamage, stats.PhysicalDamage);
        Assert.Equal(Stats.Archer.BaseAttackRange, stats.AttackRange);
        Assert.Equal(Stats.Archer.BaseAttackSpeed, stats.AttackSpeed);
        Assert.Equal(Stats.Archer.BaseCriticalChance, stats.CriticalChance);
    }

    #endregion

    #region Vocation Tests

    [Theory]
    [InlineData(VocationType.Knight, DamageType.Physical)]
    [InlineData(VocationType.Mage, DamageType.Magic)]
    [InlineData(VocationType.Archer, DamageType.Physical)]
    public void Vocation_GetPrimaryDamageType_ShouldReturnCorrectType(VocationType vocation, DamageType expectedType)
    {
        // Arrange
        var voc = new PlayerVocation(vocation);

        // Act
        var damageType = voc.GetPrimaryDamageType();

        // Assert
        Assert.Equal(expectedType, damageType);
    }

    #endregion

    #region DamageBuffer Tests

    [Fact]
    public void DamageBuffer_TryAdd_ShouldAddDamage()
    {
        // Arrange
        var buffer = new DamageBuffer();

        // Act
        var result = buffer.TryAdd(50, DamageType.Physical, 1);

        // Assert
        Assert.True(result);
        Assert.Equal(1, buffer.Count);
    }

    [Fact]
    public void DamageBuffer_TryAdd_ShouldFailWhenFull()
    {
        // Arrange
        var buffer = new DamageBuffer();
        
        // Fill buffer
        for (int i = 0; i < DamageBuffer.MaxPendingDamages; i++)
        {
            buffer.TryAdd(10, DamageType.Physical, i);
        }

        // Act
        var result = buffer.TryAdd(100, DamageType.Physical, 999);

        // Assert
        Assert.False(result);
        Assert.Equal(DamageBuffer.MaxPendingDamages, buffer.Count);
    }

    [Fact]
    public void DamageBuffer_Clear_ShouldResetCount()
    {
        // Arrange
        var buffer = new DamageBuffer();
        buffer.TryAdd(50, DamageType.Physical, 1);
        buffer.TryAdd(30, DamageType.Magic, 2);

        // Act
        buffer.Clear();

        // Assert
        Assert.Equal(0, buffer.Count);
    }

    #endregion
}
