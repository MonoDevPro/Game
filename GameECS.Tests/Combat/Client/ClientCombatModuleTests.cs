using Arch.Core;
using GameECS.Modules.Combat.Client;
using GameECS.Modules.Combat.Client.Components;
using GameECS.Modules.Combat.Shared.Data;
using Xunit;

namespace GameECS.Tests.Combat.Client;

public class ClientCombatModuleTests : IDisposable
{
    private readonly World _world;
    private readonly ClientCombatModule _module;

    public ClientCombatModuleTests()
    {
        _world = World.Create();
        _module = new ClientCombatModule(_world);
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
    public void CreateEntity_ShouldCreateClientCombatEntity(VocationType vocation)
    {
        // Act
        var entity = _module.CreateEntity(1, vocation, 100, 100, 50, 50);

        // Assert
        Assert.True(_world.Has<ClientCombatEntity>(entity));
        Assert.True(_world.Has<SyncedHealth>(entity));
        Assert.True(_world.Has<SyncedMana>(entity));
        Assert.True(_world.Has<HealthBar>(entity));
        Assert.True(_world.Has<ManaBar>(entity));
        Assert.True(_world.Has<AttackAnimation>(entity));
        Assert.True(_world.Has<FloatingDamageBuffer>(entity));
    }

    [Fact]
    public void CreateEntity_AsLocalPlayer_ShouldHaveLocalPlayerTag()
    {
        // Act
        var entity = _module.CreateEntity(1, VocationType.Knight, 100, 100, 50, 50, isLocalPlayer: true);

        // Assert
        Assert.True(_world.Has<LocalCombatPlayer>(entity));
    }

    [Fact]
    public void CreateEntity_ShouldSyncHealthCorrectly()
    {
        // Act
        var entity = _module.CreateEntity(1, VocationType.Knight, 75, 100, 30, 50);

        // Assert
        var health = _world.Get<SyncedHealth>(entity);
        Assert.Equal(75, health.Current);
        Assert.Equal(100, health.Maximum);
        Assert.Equal(0.75f, health.Percentage);
    }

    [Fact]
    public void CreateEntity_ShouldSyncManaCorrectly()
    {
        // Act
        var entity = _module.CreateEntity(1, VocationType.Mage, 100, 100, 120, 150);

        // Assert
        var mana = _world.Get<SyncedMana>(entity);
        Assert.Equal(120, mana.Current);
        Assert.Equal(150, mana.Maximum);
        Assert.Equal(0.8f, mana.Percentage);
    }

    [Fact]
    public void TryGetEntity_ShouldReturnCreatedEntity()
    {
        // Arrange
        int serverId = 42;
        var createdEntity = _module.CreateEntity(serverId, VocationType.Knight, 100, 100, 50, 50);

        // Act
        var found = _module.TryGetEntity(serverId, out var entity);

        // Assert
        Assert.True(found);
        Assert.Equal(createdEntity, entity);
    }

    [Fact]
    public void TryGetEntity_ShouldReturnFalseForUnknownId()
    {
        // Act
        var found = _module.TryGetEntity(999, out _);

        // Assert
        Assert.False(found);
    }

    [Fact]
    public void RemoveEntity_ShouldRemoveEntityMapping()
    {
        // Arrange
        int serverId = 42;
        _module.CreateEntity(serverId, VocationType.Knight, 100, 100, 50, 50);

        // Act
        _module.RemoveEntity(serverId);

        // Assert
        var found = _module.TryGetEntity(serverId, out _);
        Assert.False(found);
    }

    [Fact]
    public void AddCombatComponents_ShouldAddComponentsToExistingEntity()
    {
        // Arrange
        var entity = _world.Create();

        // Act
        _module.AddCombatComponents(entity, 1, 100, 100, 50, 50);

        // Assert
        Assert.True(_world.Has<ClientCombatEntity>(entity));
        Assert.True(_world.Has<SyncedHealth>(entity));
        Assert.True(_world.Has<HealthBar>(entity));
    }

    [Fact]
    public void OnHealthUpdated_ShouldUpdateSyncedHealth()
    {
        // Arrange
        int serverId = 1;
        _module.CreateEntity(serverId, VocationType.Knight, 100, 100, 50, 50);

        // Act
        _module.OnHealthUpdated(serverId, 50, 100, 1000);

        // Assert
        _module.TryGetEntity(serverId, out var entity);
        var health = _world.Get<SyncedHealth>(entity);
        Assert.Equal(50, health.Current);
        Assert.Equal(100, health.Maximum);
        Assert.Equal(1000, health.SyncTick);
    }

    [Fact]
    public void OnManaUpdated_ShouldUpdateSyncedMana()
    {
        // Arrange
        int serverId = 1;
        _module.CreateEntity(serverId, VocationType.Mage, 100, 100, 100, 150);

        // Act
        _module.OnManaUpdated(serverId, 75, 150, 2000);

        // Assert
        _module.TryGetEntity(serverId, out var entity);
        var mana = _world.Get<SyncedMana>(entity);
        Assert.Equal(75, mana.Current);
        Assert.Equal(150, mana.Maximum);
        Assert.Equal(2000, mana.SyncTick);
    }

    [Fact]
    public void OnDeathReceived_ShouldAddVisuallyDeadTag()
    {
        // Arrange
        int serverId = 1;
        _module.CreateEntity(serverId, VocationType.Knight, 100, 100, 50, 50);

        // Act
        _module.OnDeathReceived(new DeathNetworkMessage
        {
            EntityId = serverId,
            KillerId = 2,
            ServerTick = 1000
        });

        // Assert
        _module.TryGetEntity(serverId, out var entity);
        Assert.True(_world.Has<VisuallyDead>(entity));
    }

    [Fact]
    public void Update_ShouldNotThrowException()
    {
        // Arrange
        _module.CreateEntity(1, VocationType.Knight, 100, 100, 50, 50);
        _module.CreateEntity(2, VocationType.Mage, 80, 80, 150, 150);

        // Act & Assert - Should not throw
        var exception = Record.Exception(() =>
        {
            for (int i = 0; i < 60; i++)
            {
                _module.Update(1f / 60f);
            }
        });

        Assert.Null(exception);
    }

    [Fact]
    public void StartAttackAnimation_ShouldSetAnimationState()
    {
        // Arrange
        int attackerId = 1;
        int targetId = 2;
        _module.CreateEntity(attackerId, VocationType.Knight, 100, 100, 50, 50);

        // Act
        _module.StartAttackAnimation(attackerId, targetId, VocationType.Knight, 0.5f);

        // Assert
        _module.TryGetEntity(attackerId, out var entity);
        var animation = _world.Get<AttackAnimation>(entity);
        Assert.Equal(targetId, animation.TargetEntityId);
        Assert.Equal(VocationType.Knight, animation.AttackerVocation);
        Assert.Equal(0.5f, animation.Duration);
        Assert.False(animation.IsComplete);
    }

    [Fact]
    public void AddFloatingDamage_ShouldAddToBuffer()
    {
        // Arrange
        int targetId = 1;
        _module.CreateEntity(targetId, VocationType.Mage, 100, 100, 50, 50);

        // Act
        _module.AddFloatingDamage(targetId, 25, false, DamageType.Physical, 100f, 100f);

        // Assert
        _module.TryGetEntity(targetId, out var entity);
        var buffer = _world.Get<FloatingDamageBuffer>(entity);
        Assert.Equal(1, buffer.Count);
        
        var text = buffer.GetAt(0);
        Assert.Equal(25, text.Damage);
        Assert.False(text.IsCritical);
        Assert.Equal(DamageType.Physical, text.Type);
    }
}
