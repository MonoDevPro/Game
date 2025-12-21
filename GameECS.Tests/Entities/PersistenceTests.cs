using Arch.Core;
using GameECS.Modules.Entities.Server.Core;
using GameECS.Modules.Entities.Server.Persistence;
using GameECS.Modules.Entities.Shared.Components;
using GameECS.Modules.Navigation.Shared.Components;
using Xunit;

namespace GameECS.Tests.Entities;

public class PlayerPersistenceTests : IDisposable
{
    private readonly World _world;
    private readonly EntityFactory _factory;
    private readonly InMemoryPlayerPersistence _persistence;
    private readonly PlayerPersistenceService _service;

    public PlayerPersistenceTests()
    {
        _world = World.Create();
        _factory = new EntityFactory(_world);
        _persistence = new InMemoryPlayerPersistence();
        _service = new PlayerPersistenceService(_world, _factory, _persistence);
    }

    public void Dispose()
    {
        World.Destroy(_world);
    }

    [Fact]
    public async Task CreateCharacter_ShouldCreateNewPlayer()
    {
        // Act
        var data = await _service.CreateCharacterAsync(accountId: 1, name: "TestPlayer");

        // Assert
        Assert.NotNull(data);
        Assert.Equal(1, data.AccountId);
        Assert.Equal("TestPlayer", data.Name);
        Assert.Equal(1, data.Level);
    }

    [Fact]
    public async Task CreateCharacter_WithDuplicateName_ShouldReturnNull()
    {
        // Arrange
        await _service.CreateCharacterAsync(1, "TestPlayer");

        // Act
        var duplicate = await _service.CreateCharacterAsync(2, "TestPlayer");

        // Assert
        Assert.Null(duplicate);
    }

    [Fact]
    public async Task LoadPlayer_ShouldCreateEntityInWorld()
    {
        // Arrange
        var data = await _service.CreateCharacterAsync(1, "TestPlayer");
        Assert.NotNull(data);

        // Act
        var entity = await _service.LoadPlayerAsync(data.AccountId, data.CharacterId);

        // Assert
        Assert.NotNull(entity);
        Assert.True(_world.IsAlive(entity.Value));
        Assert.True(_world.Has<EntityIdentity>(entity.Value));
        Assert.True(_world.Has<GridPosition>(entity.Value));
    }

    [Fact]
    public async Task SavePlayer_ShouldPersistPosition()
    {
        // Arrange
        var data = await _service.CreateCharacterAsync(1, "TestPlayer");
        Assert.NotNull(data);
        var entity = await _service.LoadPlayerAsync(data.AccountId, data.CharacterId);
        Assert.NotNull(entity);

        // Move player
        var position = _world.Get<GridPosition>(entity.Value);
        position.X = 100;
        position.Y = 200;
        _world.Set(entity.Value, position);

        // Act
        bool saved = await _service.SavePlayerAsync(entity.Value);

        // Assert
        Assert.True(saved);
        var reloaded = await _persistence.LoadPlayerAsync(data.AccountId, data.CharacterId);
        Assert.NotNull(reloaded);
        Assert.Equal(100, reloaded.PositionX);
        Assert.Equal(200, reloaded.PositionY);
    }

    [Fact]
    public async Task UnloadPlayer_ShouldSaveAndRemoveFromWorld()
    {
        // Arrange
        var data = await _service.CreateCharacterAsync(1, "TestPlayer");
        Assert.NotNull(data);
        var entity = await _service.LoadPlayerAsync(data.AccountId, data.CharacterId);
        Assert.NotNull(entity);

        // Act
        await _service.UnloadPlayerAsync(entity.Value);

        // Assert
        Assert.False(_world.IsAlive(entity.Value));
    }

    [Fact]
    public async Task GetCharacters_ShouldReturnAllCharactersForAccount()
    {
        // Arrange
        await _service.CreateCharacterAsync(1, "Char1");
        await _service.CreateCharacterAsync(1, "Char2");
        await _service.CreateCharacterAsync(2, "OtherAccount");

        // Act
        var characters = await _service.GetCharactersAsync(1);

        // Assert
        Assert.Equal(2, characters.Count);
        Assert.Contains(characters, c => c.Name == "Char1");
        Assert.Contains(characters, c => c.Name == "Char2");
    }

    [Fact]
    public async Task DeleteCharacter_ShouldRemoveFromPersistence()
    {
        // Arrange
        var data = await _service.CreateCharacterAsync(1, "ToDelete");
        Assert.NotNull(data);

        // Act
        bool deleted = await _service.DeleteCharacterAsync(data.AccountId, data.CharacterId);

        // Assert
        Assert.True(deleted);
        var characters = await _service.GetCharactersAsync(1);
        Assert.Empty(characters);
    }

    [Fact]
    public async Task IsNameAvailable_ShouldReturnCorrectly()
    {
        // Arrange
        await _service.CreateCharacterAsync(1, "TakenName");

        // Act & Assert
        Assert.False(await _service.IsNameAvailableAsync("TakenName"));
        Assert.True(await _service.IsNameAvailableAsync("AvailableName"));
    }

    [Fact]
    public async Task GetEntityByCharacterId_ShouldReturnLoadedEntity()
    {
        // Arrange
        var data = await _service.CreateCharacterAsync(1, "TestPlayer");
        Assert.NotNull(data);
        var entity = await _service.LoadPlayerAsync(data.AccountId, data.CharacterId);
        Assert.NotNull(entity);

        // Act
        var found = _service.GetEntityByCharacterId(data.CharacterId);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(entity.Value, found.Value);
    }

    [Fact]
    public async Task SaveAll_ShouldSaveAllLoadedPlayers()
    {
        // Arrange
        var data1 = await _service.CreateCharacterAsync(1, "Player1");
        var data2 = await _service.CreateCharacterAsync(2, "Player2");
        Assert.NotNull(data1);
        Assert.NotNull(data2);

        var entity1 = await _service.LoadPlayerAsync(data1.AccountId, data1.CharacterId);
        var entity2 = await _service.LoadPlayerAsync(data2.AccountId, data2.CharacterId);
        Assert.NotNull(entity1);
        Assert.NotNull(entity2);

        // Modify positions
        var pos1 = _world.Get<GridPosition>(entity1.Value);
        pos1.X = 111;
        _world.Set(entity1.Value, pos1);
        var pos2 = _world.Get<GridPosition>(entity2.Value);
        pos2.X = 222;
        _world.Set(entity2.Value, pos2);

        // Act
        await _service.SaveAllAsync();

        // Assert
        var reloaded1 = await _persistence.LoadPlayerAsync(data1.AccountId, data1.CharacterId);
        var reloaded2 = await _persistence.LoadPlayerAsync(data2.AccountId, data2.CharacterId);
        Assert.Equal(111, reloaded1?.PositionX);
        Assert.Equal(222, reloaded2?.PositionX);
    }
}

public class InMemoryPetPersistenceTests
{
    private readonly InMemoryPetPersistence _persistence = new();

    [Fact]
    public async Task CreatePet_ShouldCreateNewPet()
    {
        // Act
        var pet = await _persistence.CreatePetAsync(characterId: 1, templateId: "wolf_pet", customName: "Wolfy");

        // Assert
        Assert.NotNull(pet);
        Assert.Equal(1, pet.OwnerCharacterId);
        Assert.Equal("wolf_pet", pet.TemplateId);
        Assert.Equal("Wolfy", pet.CustomName);
    }

    [Fact]
    public async Task LoadPets_ShouldReturnPetsForCharacter()
    {
        // Arrange
        await _persistence.CreatePetAsync(1, "wolf_pet");
        await _persistence.CreatePetAsync(1, "bear_pet");
        await _persistence.CreatePetAsync(2, "wolf_pet"); // Different owner

        // Act
        var pets = await _persistence.LoadPetsAsync(1);

        // Assert
        Assert.Equal(2, pets.Count);
    }

    [Fact]
    public async Task SavePet_ShouldUpdateExistingPet()
    {
        // Arrange
        var pet = await _persistence.CreatePetAsync(1, "wolf_pet");
        Assert.NotNull(pet);

        // Act
        pet.Level = 10;
        pet.CurrentHealth = 50;
        await _persistence.SavePetAsync(pet);

        // Assert
        var pets = await _persistence.LoadPetsAsync(1);
        var saved = pets.First();
        Assert.Equal(10, saved.Level);
        Assert.Equal(50, saved.CurrentHealth);
    }

    [Fact]
    public async Task DeletePet_ShouldRemovePet()
    {
        // Arrange
        var pet = await _persistence.CreatePetAsync(1, "wolf_pet");
        Assert.NotNull(pet);

        // Act
        bool deleted = await _persistence.DeletePetAsync(pet.PetId);

        // Assert
        Assert.True(deleted);
        var pets = await _persistence.LoadPetsAsync(1);
        Assert.Empty(pets);
    }
}
