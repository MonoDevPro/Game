using Arch.Core;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Entities.Server.Core;
using GameECS.Modules.Entities.Shared.Components;
using GameECS.Modules.Navigation.Shared.Components;

namespace GameECS.Modules.Entities.Server.Persistence;

/// <summary>
/// Serviço de alto nível para persistência de players.
/// Conecta o ECS com a camada de persistência.
/// </summary>
public sealed class PlayerPersistenceService
{
    private readonly World _world;
    private readonly EntityFactory _entityFactory;
    private readonly IPlayerPersistence _playerPersistence;
    private readonly IPetPersistence _petPersistence;

    // Mapeamento Entity <-> CharacterId para save/load
    private readonly Dictionary<int, Entity> _characterToEntity = new();
    private readonly Dictionary<Entity, int> _entityToCharacter = new();

    // Tracking de sessão
    private readonly Dictionary<int, DateTime> _sessionStart = new();

    public PlayerPersistenceService(
        World world,
        EntityFactory entityFactory,
        IPlayerPersistence playerPersistence,
        IPetPersistence? petPersistence = null)
    {
        _world = world;
        _entityFactory = entityFactory;
        _playerPersistence = playerPersistence;
        _petPersistence = petPersistence ?? new InMemoryPetPersistence();
    }

    /// <summary>
    /// Carrega player do banco e cria entidade no mundo.
    /// </summary>
    public async Task<Entity?> LoadPlayerAsync(int accountId, int characterId, CancellationToken ct = default)
    {
        var data = await _playerPersistence.LoadPlayerAsync(accountId, characterId, ct);
        if (data == null)
            return null;

        // Cria entidade no mundo
        var entity = _entityFactory.CreatePlayer(
            accountId: data.AccountId,
            characterId: data.CharacterId,
            name: data.Name,
            level: data.Level,
            x: data.PositionX,
            y: data.PositionY);

        // Restaura Health se existir o componente
        if (_world.Has<Health>(entity))
        {
            var health = _world.Get<Health>(entity);
            health.Current = data.CurrentHealth;
            health.Maximum = data.MaxHealth;
            _world.Set(entity, health);
        }

        // Restaura experiência
        if (_world.Has<EntityLevel>(entity))
        {
            var level = _world.Get<EntityLevel>(entity);
            level.Experience = data.Experience;
            _world.Set(entity, level);
        }

        // Tracking
        _characterToEntity[characterId] = entity;
        _entityToCharacter[entity] = characterId;
        _sessionStart[characterId] = DateTime.UtcNow;

        // Atualiza last login
        data.LastLogin = DateTime.UtcNow;
        await _playerPersistence.SavePlayerAsync(data, ct);

        return entity;
    }

    /// <summary>
    /// Salva estado atual do player no banco.
    /// </summary>
    public async Task<bool> SavePlayerAsync(Entity entity, CancellationToken ct = default)
    {
        if (!_entityToCharacter.TryGetValue(entity, out int characterId))
            return false;

        if (!_world.IsAlive(entity))
            return false;

        var identity = _world.Get<EntityIdentity>(entity);
        var ownership = _world.Get<PlayerOwnership>(entity);
        var position = _world.Get<GridPosition>(entity);
        var level = _world.Get<EntityLevel>(entity);

        var data = await _playerPersistence.LoadPlayerAsync(ownership.AccountId, characterId, ct);
        if (data == null)
            return false;

        // Atualiza dados
        data.Level = level.Level;
        data.Experience = level.Experience;
        data.PositionX = position.X;
        data.PositionY = position.Y;

        if (_world.Has<Health>(entity))
        {
            var health = _world.Get<Health>(entity);
            data.CurrentHealth = health.Current;
            data.MaxHealth = health.Maximum;
        }

        // Calcula tempo de jogo
        if (_sessionStart.TryGetValue(characterId, out var start))
        {
            data.TotalPlayTime += DateTime.UtcNow - start;
            _sessionStart[characterId] = DateTime.UtcNow; // Reset para próximo save
        }

        return await _playerPersistence.SavePlayerAsync(data, ct);
    }

    /// <summary>
    /// Salva e remove player do mundo.
    /// </summary>
    public async Task<bool> UnloadPlayerAsync(Entity entity, CancellationToken ct = default)
    {
        // Salva antes de remover
        await SavePlayerAsync(entity, ct);

        if (_entityToCharacter.TryGetValue(entity, out int characterId))
        {
            _characterToEntity.Remove(characterId);
            _entityToCharacter.Remove(entity);
            _sessionStart.Remove(characterId);
        }

        if (_world.IsAlive(entity))
            _world.Destroy(entity);

        return true;
    }

    /// <summary>
    /// Cria novo character.
    /// </summary>
    public async Task<PlayerData?> CreateCharacterAsync(int accountId, string name, CancellationToken ct = default)
    {
        return await _playerPersistence.CreatePlayerAsync(accountId, name, ct);
    }

    /// <summary>
    /// Deleta character.
    /// </summary>
    public async Task<bool> DeleteCharacterAsync(int accountId, int characterId, CancellationToken ct = default)
    {
        // Remove do mundo se estiver carregado
        if (_characterToEntity.TryGetValue(characterId, out var entity))
        {
            _characterToEntity.Remove(characterId);
            _entityToCharacter.Remove(entity);
            _sessionStart.Remove(characterId);

            if (_world.IsAlive(entity))
                _world.Destroy(entity);
        }

        return await _playerPersistence.DeletePlayerAsync(accountId, characterId, ct);
    }

    /// <summary>
    /// Lista characters da conta.
    /// </summary>
    public Task<IReadOnlyList<PlayerData>> GetCharactersAsync(int accountId, CancellationToken ct = default)
    {
        return _playerPersistence.GetCharactersAsync(accountId, ct);
    }

    /// <summary>
    /// Verifica se nome está disponível.
    /// </summary>
    public Task<bool> IsNameAvailableAsync(string name, CancellationToken ct = default)
    {
        return _playerPersistence.IsNameAvailableAsync(name, ct);
    }

    /// <summary>
    /// Obtém entidade pelo characterId.
    /// </summary>
    public Entity? GetEntityByCharacterId(int characterId)
    {
        return _characterToEntity.TryGetValue(characterId, out var entity) && _world.IsAlive(entity)
            ? entity
            : null;
    }

    /// <summary>
    /// Salva todos os players carregados.
    /// </summary>
    public async Task SaveAllAsync(CancellationToken ct = default)
    {
        foreach (var entity in _entityToCharacter.Keys.ToList())
        {
            if (_world.IsAlive(entity))
                await SavePlayerAsync(entity, ct);
        }
    }
}
