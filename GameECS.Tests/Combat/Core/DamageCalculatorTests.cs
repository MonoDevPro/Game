using GameECS.Modules.Combat.Shared.Data;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Combat.Core;
using GameECS.Shared.Combat.Data;
using GameECS.Shared.Entities.Data;
using Xunit;

namespace GameECS.Tests.Combat.Core;

public class DamageCalculatorTests
{
    #region CalculateBaseDamage Tests

    [Fact]
    public void CalculateBaseDamage_Knight_ShouldReturnPhysicalDamage()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Knight);

        // Act
        var damage = DamageCalculator.CalculateBaseDamage(stats, VocationType.Knight);

        // Assert
        Assert.Equal(stats.PhysicalDamage, damage);
    }

    [Fact]
    public void CalculateBaseDamage_Mage_ShouldReturnMagicDamage()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Mage);

        // Act
        var damage = DamageCalculator.CalculateBaseDamage(stats, VocationType.Mage);

        // Assert
        Assert.Equal(stats.MagicDamage, damage);
    }

    [Fact]
    public void CalculateBaseDamage_Archer_ShouldReturnPhysicalDamage()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Archer);

        // Act
        var damage = DamageCalculator.CalculateBaseDamage(stats, VocationType.Archer);

        // Assert
        Assert.Equal(stats.PhysicalDamage, damage);
    }

    #endregion

    #region CalculateDefense Tests

    [Fact]
    public void CalculateDefense_PhysicalDamage_ShouldReturnPhysicalDefense()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Knight);

        // Act
        var defense = DamageCalculator.CalculateDefense(stats, DamageType.Physical);

        // Assert
        Assert.Equal(stats.PhysicalDefense, defense);
    }

    [Fact]
    public void CalculateDefense_MagicDamage_ShouldReturnMagicDefense()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Mage);

        // Act
        var defense = DamageCalculator.CalculateDefense(stats, DamageType.Magic);

        // Assert
        Assert.Equal(stats.MagicDefense, defense);
    }

    [Fact]
    public void CalculateDefense_TrueDamage_ShouldReturnZero()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Knight);

        // Act
        var defense = DamageCalculator.CalculateDefense(stats, DamageType.True);

        // Assert
        Assert.Equal(0, defense);
    }

    #endregion

    #region ApplyDefenseMitigation Tests

    [Fact]
    public void ApplyDefenseMitigation_WithZeroDefense_ShouldReturnFullDamage()
    {
        // Act
        var finalDamage = DamageCalculator.ApplyDefenseMitigation(100, 0);

        // Assert
        Assert.Equal(100, finalDamage);
    }

    [Fact]
    public void ApplyDefenseMitigation_WithDefense_ShouldReduceDamage()
    {
        // Act - 100 defense should reduce damage by ~50%
        var finalDamage = DamageCalculator.ApplyDefenseMitigation(100, 100);

        // Assert
        Assert.Equal(50, finalDamage);
    }

    [Fact]
    public void ApplyDefenseMitigation_ShouldNeverReturnZero()
    {
        // Act - Even with very high defense, minimum damage is 1
        var finalDamage = DamageCalculator.ApplyDefenseMitigation(10, 10000);

        // Assert
        Assert.True(finalDamage >= 1);
    }

    #endregion

    #region ApplyCriticalDamage Tests

    [Fact]
    public void ApplyCriticalDamage_ShouldMultiplyDamage()
    {
        // Act
        var critDamage = DamageCalculator.ApplyCriticalDamage(100, 1.5f);

        // Assert
        Assert.Equal(150, critDamage);
    }

    [Fact]
    public void ApplyCriticalDamage_With2xMultiplier_ShouldDoubleDamage()
    {
        // Act
        var critDamage = DamageCalculator.ApplyCriticalDamage(50, 2f);

        // Assert
        Assert.Equal(100, critDamage);
    }

    #endregion

    #region CalculateDistance Tests

    [Fact]
    public void CalculateDistance_SamePosition_ShouldReturnZero()
    {
        // Act
        var distance = DamageCalculator.CalculateDistance(5, 5, 5, 5);

        // Assert
        Assert.Equal(0, distance);
    }

    [Fact]
    public void CalculateDistance_CardinalDirection_ShouldReturnCorrectDistance()
    {
        // Act - Moving 3 cells to the right
        var distance = DamageCalculator.CalculateDistance(0, 0, 3, 0);

        // Assert
        Assert.Equal(3, distance);
    }

    [Fact]
    public void CalculateDistance_DiagonalDirection_ShouldReturnChebyshevDistance()
    {
        // Act - Moving 3 cells diagonal (Chebyshev distance)
        var distance = DamageCalculator.CalculateDistance(0, 0, 3, 3);

        // Assert
        Assert.Equal(3, distance);
    }

    #endregion

    #region IsInRange Tests

    [Fact]
    public void IsInRange_WithinRange_ShouldReturnTrue()
    {
        // Act
        var inRange = DamageCalculator.IsInRange(0, 0, 2, 2, 5);

        // Assert
        Assert.True(inRange);
    }

    [Fact]
    public void IsInRange_OutOfRange_ShouldReturnFalse()
    {
        // Act
        var inRange = DamageCalculator.IsInRange(0, 0, 10, 10, 5);

        // Assert
        Assert.False(inRange);
    }

    [Fact]
    public void IsInRange_AtExactRange_ShouldReturnTrue()
    {
        // Act
        var inRange = DamageCalculator.IsInRange(0, 0, 5, 0, 5);

        // Assert
        Assert.True(inRange);
    }

    #endregion

    #region CalculateAttackCooldown Tests

    [Fact]
    public void CalculateAttackCooldown_NormalSpeed_ShouldReturnBaseCooldown()
    {
        // Act
        var cooldown = DamageCalculator.CalculateAttackCooldown(60, 1.0f);

        // Assert
        Assert.Equal(60, cooldown);
    }

    [Fact]
    public void CalculateAttackCooldown_FastSpeed_ShouldReduceCooldown()
    {
        // Act - 1.5x attack speed should reduce cooldown by ~33%
        var cooldown = DamageCalculator.CalculateAttackCooldown(60, 1.5f);

        // Assert
        Assert.Equal(40, cooldown);
    }

    [Fact]
    public void CalculateAttackCooldown_SlowSpeed_ShouldIncreaseCooldown()
    {
        // Act - 0.5x attack speed should double cooldown
        var cooldown = DamageCalculator.CalculateAttackCooldown(60, 0.5f);

        // Assert
        Assert.Equal(120, cooldown);
    }

    [Fact]
    public void CalculateAttackCooldown_ShouldNeverReturnZero()
    {
        // Act - Very high attack speed
        var cooldown = DamageCalculator.CalculateAttackCooldown(10, 100f);

        // Assert
        Assert.True(cooldown >= 1);
    }

    #endregion

    #region CalculateFullDamage Tests

    [Fact]
    public void CalculateFullDamage_ShouldReturnValidDamageInfo()
    {
        // Arrange
        var attackerStats = CombatStats.FromVocation(VocationType.Knight);
        var targetStats = CombatStats.FromVocation(VocationType.Mage);

        // Act
        var damageInfo = DamageCalculator.CalculateFullDamage(
            attackerStats,
            targetStats,
            VocationType.Knight,
            1.5f,
            1,
            2,
            100);

        // Assert
        Assert.True(damageInfo.FinalDamage > 0);
        Assert.Equal(DamageType.Physical, damageInfo.Type);
        Assert.Equal(1, damageInfo.AttackerId);
        Assert.Equal(2, damageInfo.TargetId);
        Assert.Equal(100, damageInfo.Tick);
    }

    [Fact]
    public void CalculateFullDamage_MageVsKnight_ShouldUseMagicDamageType()
    {
        // Arrange
        var attackerStats = CombatStats.FromVocation(VocationType.Mage);
        var targetStats = CombatStats.FromVocation(VocationType.Knight);

        // Act
        var damageInfo = DamageCalculator.CalculateFullDamage(
            attackerStats,
            targetStats,
            VocationType.Mage,
            1.5f,
            1,
            2,
            100);

        // Assert
        Assert.Equal(DamageType.Magic, damageInfo.Type);
    }

    #endregion

    #region CalculateVocationDamage Tests

    [Fact]
    public void CalculateVocationDamage_Knight_ShouldUsePhysicalDamage()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Knight);

        // Act
        var damage = DamageCalculator.CalculateVocationDamage(
            VocationType.Knight,
            stats,
            0,
            DamageType.Physical);

        // Assert
        Assert.Equal(stats.PhysicalDamage, damage);
    }

    [Fact]
    public void CalculateVocationDamage_Mage_ShouldHaveBonus()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Mage);

        // Act
        var damage = DamageCalculator.CalculateVocationDamage(
            VocationType.Mage,
            stats,
            0,
            DamageType.Magic);

        // Assert - Mage has 10% bonus
        Assert.Equal((int)(stats.MagicDamage * 1.1f), damage);
    }

    [Fact]
    public void CalculateVocationDamage_Archer_ShouldScaleWithAttackSpeed()
    {
        // Arrange
        var stats = CombatStats.FromVocation(VocationType.Archer);

        // Act
        var damage = DamageCalculator.CalculateVocationDamage(
            VocationType.Archer,
            stats,
            0,
            DamageType.Physical);

        // Assert - Archer scales with attack speed
        Assert.Equal((int)(stats.PhysicalDamage * stats.AttackSpeed), damage);
    }

    #endregion
}
