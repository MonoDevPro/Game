using Arch.Core;
using GameECS.Modules.Entities.Shared.Data;
using GameECS.Server.Entities.Core;
using GameECS.Shared.Entities.Components;
using GameECS.Shared.Entities.Data;
using GameECS.Shared.Navigation.Components;
using Xunit;

namespace GameECS.Tests.Entities;

public class EntityFactoryTests : IDisposable
{
    private readonly World _world;
    private readonly EntityFactory _factory;

    public EntityFactoryTests()
    {
        _world = World.Create();
        _factory = new EntityFactory(_world);
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    [Fact]
    public void CreatePlayer_ShouldCreateEntityWithCorrectComponents()
    {
        // Act
        var entity = _factory.CreatePlayer(
            accountId: 100,
            characterId: 1,
            name: "TestPlayer",
            level: 10,
            x: 50,
            y: 50);

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<Identity>(entity));
        Assert.True(_world.Has<Name>(entity));
        Assert.True(_world.Has<Level>(entity));
        Assert.True(_world.Has<PlayerOwnership>(entity));
        Assert.True(_world.Has<GridPosition>(entity));

        ref var identity = ref _world.Get<Identity>(entity);
        Assert.Equal(EntityType.Player, identity.Type);
        Assert.Equal(1, identity.UniqueId);

        ref var ownership = ref _world.Get<PlayerOwnership>(entity);
        Assert.Equal(100, ownership.AccountId);
        Assert.Equal(1, ownership.CharacterId);

        ref var level = ref _world.Get<Level>(entity);
        Assert.Equal(10, level.Lvl);

        ref var position = ref _world.Get<GridPosition>(entity);
        Assert.Equal(50, position.X);
        Assert.Equal(50, position.Y);
    }

    [Fact]
    public void CreateNpc_ShouldCreateEntityFromTemplate()
    {
        // Act
        var entity = _factory.CreateNpc("rat", x: 10, y: 20);

        // Assert
        Assert.True(_world.IsAlive(entity));
        Assert.True(_world.Has<Identity>(entity));
        Assert.True(_world.Has<NpcBehavior>(entity));
        Assert.True(_world.Has<NpcAI>(entity));
        Assert.True(_world.Has<SpawnInfo>(entity));

        ref var identity = ref _world.Get<Identity>(entity);
        Assert.Equal(EntityType.Npc, identity.Type);

        ref var behavior = ref _world.Get<NpcBehavior>(entity);
        Assert.Equal(NpcSubType.Hostile, behavior.SubType);

        ref var spawn = ref _world.Get<SpawnInfo>(entity);
        Assert.Equal(10, spawn.SpawnX);
        Assert.Equal(20, spawn.SpawnY);
    }

    [Fact]
    public void CreateNpc_WithInvalidTemplate_ShouldThrow()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => _factory.CreateNpc("invalid_template", 0, 0));
    }

    [Fact]
    public void CreatePet_ShouldCreateEntityWithOwnership()
    {
        // Arrange
        var owner = _factory.CreatePlayer(1, 1, "Owner", 1, 0, 0);
        ref var ownerId = ref _world.Get<Identity>(owner);

        // Act
        var pet = _factory.CreatePet("wolf_pet", ownerId.UniqueId, x: 5, y: 5);

        // Assert
        Assert.True(_world.IsAlive(pet));
        Assert.True(_world.Has<PetOwnership>(pet));
        Assert.True(_world.Has<PetBehavior>(pet));
        Assert.True(_world.Has<PetState>(pet));

        ref var ownership = ref _world.Get<PetOwnership>(pet);
        Assert.Equal(ownerId.UniqueId, ownership.OwnerEntityId);
        Assert.True(ownership.IsActive);
    }

    [Fact]
    public void EntityIds_ShouldBeUnique()
    {
        // Act
        var player1 = _factory.CreatePlayer(1, 1, "P1", 1, 0, 0);
        var player2 = _factory.CreatePlayer(2, 2, "P2", 1, 10, 10);
        var npc = _factory.CreateNpc("wolf", 20, 20);

        // Assert
        ref var id1 = ref _world.Get<Identity>(player1);
        ref var id2 = ref _world.Get<Identity>(player2);
        ref var id3 = ref _world.Get<Identity>(npc);

        Assert.NotEqual(id1.UniqueId, id2.UniqueId);
        Assert.NotEqual(id2.UniqueId, id3.UniqueId);
        Assert.NotEqual(id1.UniqueId, id3.UniqueId);
    }
}

public class PartyManagerTests : IDisposable
{
    private readonly World _world;
    private readonly EntityFactory _factory;
    private readonly PartyManager _partyManager;

    public PartyManagerTests()
    {
        _world = World.Create();
        _factory = new EntityFactory(_world);
        _partyManager = new PartyManager();
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    [Fact]
    public void CreateParty_ShouldReturnValidPartyId()
    {
        // Arrange
        var leader = _factory.CreatePlayer(1, 1, "Leader", 10, 0, 0);

        // Act
        int partyId = _partyManager.CreateParty(leader);

        // Assert
        Assert.True(partyId > 0);
        Assert.Equal(1, _partyManager.PartyCount);
    }

    [Fact]
    public void AddMember_ShouldAddToParty()
    {
        // Arrange
        var leader = _factory.CreatePlayer(1, 1, "Leader", 10, 0, 0);
        var member = _factory.CreatePlayer(2, 2, "Member", 10, 10, 10);
        int partyId = _partyManager.CreateParty(leader);

        // Act
        bool added = _partyManager.AddMember(partyId, member);

        // Assert
        Assert.True(added);
        var members = _partyManager.GetMembers(partyId);
        Assert.Equal(2, members.Count);
    }

    [Fact]
    public void Party_ShouldRespectMaxMembers()
    {
        // Arrange
        var config = new PartyConfig { MaxMembers = 2 };
        var leader = _factory.CreatePlayer(1, 1, "Leader", 10, 0, 0);
        var member1 = _factory.CreatePlayer(2, 2, "M1", 10, 10, 10);
        var member2 = _factory.CreatePlayer(3, 3, "M2", 10, 20, 20);

        int partyId = _partyManager.CreateParty(leader, config);
        _partyManager.AddMember(partyId, member1);

        // Act
        bool added = _partyManager.AddMember(partyId, member2);

        // Assert
        Assert.False(added);
    }

    [Fact]
    public void RemoveMember_ShouldRemoveFromParty()
    {
        // Arrange
        var leader = _factory.CreatePlayer(1, 1, "Leader", 10, 0, 0);
        var member = _factory.CreatePlayer(2, 2, "Member", 10, 10, 10);
        int partyId = _partyManager.CreateParty(leader);
        _partyManager.AddMember(partyId, member);

        // Act
        bool removed = _partyManager.RemoveMember(partyId, member);

        // Assert
        Assert.True(removed);
        var members = _partyManager.GetMembers(partyId);
        Assert.Single(members);
    }

    [Fact]
    public void DissolveParty_ShouldRemoveParty()
    {
        // Arrange
        var leader = _factory.CreatePlayer(1, 1, "Leader", 10, 0, 0);
        int partyId = _partyManager.CreateParty(leader);

        // Act
        bool dissolved = _partyManager.DissolveParty(partyId);

        // Assert
        Assert.True(dissolved);
        Assert.Equal(0, _partyManager.PartyCount);
    }
}

public class AOIManagerTests : IDisposable
{
    private readonly World _world;
    private readonly EntityFactory _factory;
    private readonly AOIManager _aoiManager;

    public AOIManagerTests()
    {
        _world = World.Create();
        _factory = new EntityFactory(_world);
        _aoiManager = new AOIManager(_world);
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    [Fact]
    public void UpdateVisibility_ShouldDetectNearbyEntities()
    {
        // Arrange
        var observer = _factory.CreatePlayer(1, 1, "Observer", 10, 50, 50);
        var nearby = _factory.CreateNpc("rat", 55, 55); // Within view
        var faraway = _factory.CreateNpc("wolf", 100, 100); // Out of view

        var position = new GridPosition(50, 50);
        var config = new VisibilityConfig { ViewRadius = 10, MaxVisibleEntities = 100, UpdateRate = 1 };

        // Act
        var result = _aoiManager.UpdateVisibility(observer, position, config);

        // Assert
        Assert.Equal(1, result.VisibleCount);
        Assert.Single(result.EnteredView);
    }

    [Fact]
    public void UpdateVisibility_ShouldDetectEntitiesLeavingView()
    {
        // Arrange
        var observer = _factory.CreatePlayer(1, 1, "Observer", 10, 50, 50);
        var entity = _factory.CreateNpc("rat", 55, 55);

        var config = new VisibilityConfig { ViewRadius = 10, MaxVisibleEntities = 100, UpdateRate = 1 };

        // First update - entity enters view
        _aoiManager.UpdateVisibility(observer, new GridPosition(50, 50), config);

        // Move entity far away
        ref var entityPos = ref _world.Get<GridPosition>(entity);
        entityPos.X = 200;
        entityPos.Y = 200;

        // Act - second update, entity should leave view
        var result = _aoiManager.UpdateVisibility(observer, new GridPosition(50, 50), config);

        // Assert
        Assert.Equal(0, result.VisibleCount);
        Assert.Single(result.LeftViewIds);
    }
}
