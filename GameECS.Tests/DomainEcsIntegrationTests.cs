using Arch.Core;
using Game.Domain.AI.Enums;
using Game.Domain.AI.ValueObjects;
using Game.Domain.AOI.ValueObjects;
using Game.Domain.Combat;
using Game.Domain.Combat.Core;
using Game.Domain.Combat.Enums;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Attributes;
using Game.Domain.Commons.ValueObjects.Character;
using Game.Domain.Commons.ValueObjects.Equipment;
using Game.Domain.Commons.ValueObjects.Identitys;
using Game.Domain.Commons.ValueObjects.Map;
using Game.Domain.Commons.ValueObjects.Vitals;
using Game.Domain.Player;
using Game.Domain.Player.ValueObjects;
using Game.Domain.Vocations.ValueObjects;
using GameECS.Core;
using GameECS.Shared.Entities.Data;
using Xunit;

namespace GameECS.Tests;

/// <summary>
/// Testes de integração entre Domain e ECS.
/// </summary>
public class DomainEcsIntegrationTests : IDisposable
{
    private readonly World _world;

    public DomainEcsIntegrationTests()
    {
        _world = World.Create();
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    [Fact]
    public void EntityFactory_CreatePlayer_ShouldCreateEntityWithComponents()
    {
        // Arrange
        var factory = new EntityFactory(_world);
        var attributes = CreateTestPlayerAttributes();

        // Act
        var entity = factory.CreatePlayer(attributes);

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<Identity>(entity));
        Assert.True(_world.Has<Name>(entity));
        Assert.True(_world.Has<Vocation>(entity));
        Assert.True(_world.Has<Health>(entity));
        Assert.True(_world.Has<GridPosition>(entity));

        ref var identity = ref _world.Get<Identity>(entity);
        Assert.Equal(EntityType.Player, identity.Type);
        Assert.True(identity.UniqueId > 0);
    }

    [Fact]
    public void EntityFactory_CreateNpc_ShouldCreateEntityWithAIComponents()
    {
        // Arrange
        var factory = new EntityFactory(_world, new DefaultNpcTemplateProvider());

        // Act
        var entity = factory.CreateNpc("rat", 10, 20);

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<Identity>(entity));
        Assert.True(_world.Has<NpcAI>(entity));
        Assert.True(_world.Has<NpcBehavior>(entity));
        Assert.True(_world.Has<AggroTable>(entity));
        Assert.True(_world.Has<SpawnInfo>(entity));

        ref var identity = ref _world.Get<Identity>(entity);
        Assert.Equal(EntityType.Npc, identity.Type);

        ref var spawn = ref _world.Get<SpawnInfo>(entity);
        Assert.Equal(10, spawn.SpawnX);
        Assert.Equal(20, spawn.SpawnY);
    }

    [Fact]
    public void EntityFactory_CreatePet_ShouldCreateEntityWithPetComponents()
    {
        // Arrange
        var factory = new EntityFactory(_world);

        // Act
        var entity = factory.CreatePet("wolf_pet", ownerEntityId: 1, x: 5, y: 5);

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<Identity>(entity));
        Assert.True(_world.Has<PetOwnership>(entity));
        Assert.True(_world.Has<PetBehavior>(entity));
        Assert.True(_world.Has<PetState>(entity));

        ref var ownership = ref _world.Get<PetOwnership>(entity);
        Assert.Equal(1, ownership.OwnerEntityId);
        Assert.True(ownership.IsActive);
    }

    [Fact]
    public void AOIManager_UpdateVisibility_ShouldTrackVisibleEntities()
    {
        // Arrange
        var aoiManager = new AOIManager(_world);

        var observer = _world.Create(
            new Identity { UniqueId = 1, Type = EntityType.Player },
            new GridPosition(0, 0),
            VisibilityConfig.ForPlayer
        );

        _world.Create(
            new Identity { UniqueId = 2, Type = EntityType.Npc },
            new GridPosition(5, 5)
        );

        _world.Create(
            new Identity { UniqueId = 3, Type = EntityType.Npc },
            new GridPosition(100, 100)
        );

        // Act
        var observerPos = _world.Get<GridPosition>(observer);
        var config = _world.Get<VisibilityConfig>(observer);
        aoiManager.UpdateVisibility(observer, in observerPos, in config);

        // Assert
        Assert.Contains(2, aoiManager.GetVisibleEntities(1));
        Assert.DoesNotContain(3, aoiManager.GetVisibleEntities(1));
    }

    [Fact]
    public void PartyManager_CreateAndManageParty_ShouldWork()
    {
        // Arrange
        var partyManager = new PartyManager(_world);

        var leader = _world.Create(
            new Identity { UniqueId = 1, Type = EntityType.Player }
        );

        var member = _world.Create(
            new Identity { UniqueId = 2, Type = EntityType.Player }
        );

        // Act
        var partyId = partyManager.CreateParty(leader);
        var added = partyManager.AddMember(partyId, member);

        // Assert
        Assert.True(partyId > 0);
        Assert.True(added);
        Assert.Equal(2, partyManager.GetMembers(partyId).Count);
        Assert.Equal(1, partyManager.PartyCount);
    }

    [Fact]
    public void DamageCalculator_CalculateFullDamage_ShouldReturnValidDamage()
    {
        // Arrange
        var attackerStats = new CombatStats(
            PhysicalAttack: 100,
            MagicAttack: 50,
            PhysicalDefense: 20,
            MagicDefense: 10,
            AttackRange: 1,
            AttackSpeed: 1.0,
            CriticalChance: 10,
            CriticalDamage: 150,
            ManaCostPerAttack: 0,
            DamageType: DamageType.Physical);

        var targetStats = new CombatStats(
            PhysicalAttack: 50,
            MagicAttack: 25,
            PhysicalDefense: 30,
            MagicDefense: 15,
            AttackRange: 1,
            AttackSpeed: 1.0,
            CriticalChance: 5,
            CriticalDamage: 150,
            ManaCostPerAttack: 0,
            DamageType: DamageType.Physical);

        // Act
        var damage = DamageCalculator.CalculateFullDamage(
            in attackerStats,
            in targetStats,
            critMultiplier: 1.5,
            attackerId: 1,
            targetId: 2);

        // Assert
        Assert.Equal(1, damage.AttackerId);
        Assert.Equal(2, damage.TargetId);
        Assert.True(damage.FinalDamage > 0);
        Assert.Equal(DamageType.Physical, damage.DamageType);
    }

    [Fact]
    public void CombatConfig_DefaultPresets_ShouldBeValid()
    {
        // Act
        var defaultConfig = CombatConfig.Default;
        var pvpConfig = CombatConfig.PvPBalanced;
        var pveConfig = CombatConfig.PvE;

        // Assert
        Assert.True(defaultConfig.BaseAttackCooldownTicks > 0);
        Assert.True(defaultConfig.CriticalDamageMultiplier > 1.0);

        Assert.True(pvpConfig.BaseAttackCooldownTicks > 0);
        Assert.False(pvpConfig.AllowFriendlyFire);

        Assert.True(pveConfig.CriticalDamageMultiplier >= defaultConfig.CriticalDamageMultiplier);
    }

    private static PlayerSimulationAttributes CreateTestPlayerAttributes()
    {
        // Cria componentes individualmente já que não há factory method completo
        var ownership = new PlayerOwnership
        {
            AccountId = 1,
            CharacterId = 1
        };

        var name = Name.Create("TestPlayer");
        var vocation = Vocation.Create(VocationType.Warrior);
        var progress = Progress.Create(level: 10, experience: 1000);
        var equipment = Equipment.Empty;

        var baseStats = new BaseStats(
            Strength: 20,
            Dexterity: 15,
            Intelligence: 10,
            Constitution: 18,
            Spirit: 12);

        var combatStats = CombatStats.BuildFrom(baseStats, vocation);
        var health = Health.Create(baseStats, progress);
        var mana = Mana.Create(baseStats, progress);
        var position = new GridPosition(100, 100);

        // Usa reflection ou criar método de teste
        return CreateFromValues(ownership, name, vocation, progress, equipment, baseStats, combatStats, health, mana, position);
    }

    private static PlayerSimulationAttributes CreateFromValues(
        PlayerOwnership ownership,
        Name name,
        Vocation vocation,
        Progress progress,
        Equipment equipment,
        BaseStats baseStats,
        CombatStats combatStats,
        Health health,
        Mana mana,
        GridPosition position)
    {
        // Usa reflection para criar instância já que o construtor é privado
        var type = typeof(PlayerSimulationAttributes);
        var constructor = type.GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];
        return (PlayerSimulationAttributes)constructor.Invoke(new object[]
        {
            ownership, name, vocation, progress, equipment, baseStats, combatStats, health, mana, position
        });
    }
}
