namespace Game.ECS.Shared.Services.Entities;

/// <summary>
/// Índice bidirecional entre uma chave genérica TKey <-> Entity (struct do Arch).
/// Thread-safe: leituras lock-free, escritas com lock para manter os dois dicionários consistentes.
/// </summary>
/// <typeparam name="TKey">Tipo da chave (ex.: int, Guid, string, ulong). Deve ser notnull.</typeparam>
public class EntityIndex<TKey> where TKey : notnull
{
    // key -> Entity (capturamos Entity.Id e Entity.Version)
    private readonly Dictionary<TKey, Arch.Core.Entity> _keyToEntity = new();

    // entityId -> key (reverse lookup rápido)
    private readonly Dictionary<int, TKey> _entityIdToKey = new();

    /// <inheritdoc/>
    public int Count => _keyToEntity.Count;

    /// <inheritdoc/>
    public void Register(TKey key, Arch.Core.Entity entity)
    {
        if (_keyToEntity.TryGetValue(key, out var existing) && existing.Id != entity.Id)
            _entityIdToKey.Remove(existing.Id, out _);

        _keyToEntity[key] = entity;
        _entityIdToKey[entity.Id] = key;
    }

    /// <inheritdoc/>
    public bool TryRegisterUnique(TKey key, Arch.Core.Entity entity)
    {
        if (!_keyToEntity.TryAdd(key, entity))
            return false;

        _entityIdToKey[entity.Id] = key;
        return true;
    }

    /// <inheritdoc/>
    public void RemoveByKey(TKey key)
    {
        if (_keyToEntity.Remove(key, out var entity))
            _entityIdToKey.Remove(entity.Id, out _);
    }

    /// <inheritdoc/>
    public void RemoveByEntity(Arch.Core.Entity entity)
    {
        if (_entityIdToKey.Remove(entity.Id, out var key))
            _keyToEntity.Remove(key, out _);
    }

    /// <inheritdoc/>
    public bool TryGetEntity(TKey key, out Arch.Core.Entity entity)
    {
        return _keyToEntity.TryGetValue(key, out entity);
    }

    /// <inheritdoc/>
    public bool TryGetKeyByEntityId(int entityId, out TKey? key)
    {
        return _entityIdToKey.TryGetValue(entityId, out key);
    }
    
    /// <inheritdoc/>
    public bool TryGetKeyByEntity(Arch.Core.Entity entity, out TKey? key)
    {
        return _entityIdToKey.TryGetValue(entity.Id, out key);
    }

    /// <inheritdoc/>
    public bool TryUpdateEntity(TKey key, Arch.Core.Entity newEntity)
    {
        if (!_keyToEntity.TryGetValue(key, out var existing))
            return false;

        if (existing.Id != newEntity.Id)
        {
            // mudou o id — atualizar reverse mapping
            _entityIdToKey.Remove(existing.Id, out _);
            _entityIdToKey[newEntity.Id] = key;
        }
        else
        {
            // mesma id — apenas confirma/update reverse mapping (safe)
            _entityIdToKey[newEntity.Id] = key;
        }

        _keyToEntity[key] = newEntity;
        return true;
    }

    /// <inheritdoc/>
    public bool TryRemoveByKeyIfVersionMatches(TKey key, int expectedVersion)
    {
        if (_keyToEntity.TryGetValue(key, out var entity) && entity.Version == expectedVersion)
        {
            _keyToEntity.Remove(key, out _);
            _entityIdToKey.Remove(entity.Id, out _);
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public bool TryRemoveByEntityIfVersionMatches(Arch.Core.Entity entity, int expectedVersion)
    {
        if (_entityIdToKey.TryGetValue(entity.Id, out var key) &&
            _keyToEntity.TryGetValue(key, out var stored) &&
            stored.Version == expectedVersion)
        {
            _keyToEntity.Remove(key, out _);
            _entityIdToKey.Remove(entity.Id, out _);
            return true;
        }
        return false;
    }

    /// <inheritdoc/>
    public void Clear()
    {
        _keyToEntity.Clear();
        _entityIdToKey.Clear();
    }

    /// <inheritdoc/>
    public IReadOnlyDictionary<TKey, Arch.Core.Entity> Snapshot()
    {
        return _keyToEntity.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <inheritdoc/>
    public void RebuildFrom(IEnumerable<(TKey key, Arch.Core.Entity entity)> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        _keyToEntity.Clear();
        _entityIdToKey.Clear();

        foreach (var (key, entity) in items)
        {
            _keyToEntity[key] = entity;
            _entityIdToKey[entity.Id] = key;
        }
    }
}