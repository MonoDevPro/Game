using System.Collections.Concurrent;

namespace GameECS.Modules.Entities.Server.Persistence;

/// <summary>
/// Implementação em memória para testes e desenvolvimento.
/// </summary>
public sealed class InMemoryPlayerPersistence : IPlayerPersistence
{
    private readonly ConcurrentDictionary<(int AccountId, int CharacterId), PlayerData> _players = new();
    private readonly ConcurrentDictionary<string, bool> _usedNames = new(StringComparer.OrdinalIgnoreCase);
    private int _nextCharacterId;

    public Task<PlayerData?> LoadPlayerAsync(int accountId, int characterId, CancellationToken ct = default)
    {
        _players.TryGetValue((accountId, characterId), out var data);
        return Task.FromResult(data);
    }

    public Task<bool> SavePlayerAsync(PlayerData data, CancellationToken ct = default)
    {
        data.LastSave = DateTime.UtcNow;
        _players[(data.AccountId, data.CharacterId)] = data;
        return Task.FromResult(true);
    }

    public Task<PlayerData?> CreatePlayerAsync(int accountId, string name, CancellationToken ct = default)
    {
        if (!_usedNames.TryAdd(name, true))
            return Task.FromResult<PlayerData?>(null);

        int characterId = Interlocked.Increment(ref _nextCharacterId);

        var data = new PlayerData
        {
            AccountId = accountId,
            CharacterId = characterId,
            Name = name,
            Level = 1,
            Experience = 0,
            PositionX = 50,
            PositionY = 50,
            MapId = 1,
            CurrentHealth = 100,
            MaxHealth = 100,
            CurrentMana = 50,
            MaxMana = 50,
            LastLogin = DateTime.UtcNow,
            LastSave = DateTime.UtcNow,
            TotalPlayTime = TimeSpan.Zero
        };

        _players[(accountId, characterId)] = data;
        return Task.FromResult<PlayerData?>(data);
    }

    public Task<bool> DeletePlayerAsync(int accountId, int characterId, CancellationToken ct = default)
    {
        if (_players.TryRemove((accountId, characterId), out var data))
        {
            _usedNames.TryRemove(data.Name, out _);
            return Task.FromResult(true);
        }
        return Task.FromResult(false);
    }

    public Task<IReadOnlyList<PlayerData>> GetCharactersAsync(int accountId, CancellationToken ct = default)
    {
        var characters = _players.Values
            .Where(p => p.AccountId == accountId)
            .OrderBy(p => p.CharacterId)
            .ToList();

        return Task.FromResult<IReadOnlyList<PlayerData>>(characters);
    }

    public Task<bool> IsNameAvailableAsync(string name, CancellationToken ct = default)
    {
        return Task.FromResult(!_usedNames.ContainsKey(name));
    }
}

/// <summary>
/// Implementação em memória de persistência de pets.
/// </summary>
public sealed class InMemoryPetPersistence : IPetPersistence
{
    private readonly ConcurrentDictionary<int, PetData> _pets = new();
    private int _nextPetId;

    public Task<IReadOnlyList<PetData>> LoadPetsAsync(int characterId, CancellationToken ct = default)
    {
        var pets = _pets.Values
            .Where(p => p.OwnerCharacterId == characterId)
            .OrderBy(p => p.PetId)
            .ToList();

        return Task.FromResult<IReadOnlyList<PetData>>(pets);
    }

    public Task<bool> SavePetAsync(PetData data, CancellationToken ct = default)
    {
        _pets[data.PetId] = data;
        return Task.FromResult(true);
    }

    public Task<PetData?> CreatePetAsync(int characterId, string templateId, string? customName = null, CancellationToken ct = default)
    {
        int petId = Interlocked.Increment(ref _nextPetId);

        var data = new PetData
        {
            PetId = petId,
            OwnerCharacterId = characterId,
            TemplateId = templateId,
            CustomName = customName,
            Level = 1,
            Experience = 0,
            IsActive = false,
            CurrentHealth = 100,
            MaxHealth = 100
        };

        _pets[petId] = data;
        return Task.FromResult<PetData?>(data);
    }

    public Task<bool> DeletePetAsync(int petId, CancellationToken ct = default)
    {
        return Task.FromResult(_pets.TryRemove(petId, out _));
    }
}
