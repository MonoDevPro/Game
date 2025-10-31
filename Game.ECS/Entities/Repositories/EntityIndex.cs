using Arch.Core;

// ajuste se seu Entity estiver em outro namespace

namespace Game.ECS.Entities.Repositories;

/// <summary>
/// Índice bidirecional entre uma chave genérica TKey <-> Entity (struct do Arch).
/// Thread-safe: leituras lock-free, escritas com lock para manter os dois dicionários consistentes.
/// </summary>
/// <typeparam name="TKey">Tipo da chave (ex.: int, Guid, string, ulong). Deve ser notnull.</typeparam>
public abstract class EntityIndex<TKey> where TKey : notnull
{
    // key -> Entity (capturamos Entity.Id e Entity.Version)
    private readonly Dictionary<TKey, Entity> _keyToEntity = new();

    // entityId -> key (reverse lookup rápido)
    private readonly Dictionary<int, TKey> _entityIdToKey = new();

    public int Count => _keyToEntity.Count;

    /// <summary>
    /// Adiciona ou atualiza o mapeamento key -> entity.
    /// Substitui a entrada anterior, removendo o reverse mapping antigo se necessário.
    /// </summary>
    public void AddMapping(TKey key, Entity entity)
    {
        if (_keyToEntity.TryGetValue(key, out var existing) && existing.Id != entity.Id)
            _entityIdToKey.Remove(existing.Id, out _);

        _keyToEntity[key] = entity;
        _entityIdToKey[entity.Id] = key;
    }

    /// <summary>
    /// Tenta adicionar apenas se a chave ainda não existir. Retorna true se adicionou.
    /// </summary>
    public bool TryAddMappingUnique(TKey key, Entity entity)
    {
        if (!_keyToEntity.TryAdd(key, entity))
            return false;

        _entityIdToKey[entity.Id] = key;
        return true;
    }

    /// <summary>
    /// Remove mapeamento por chave (se existir).
    /// </summary>
    public void RemoveByKey(TKey key)
    {
        if (_keyToEntity.Remove(key, out var entity))
            _entityIdToKey.Remove(entity.Id, out _);
    }

    /// <summary>
    /// Remove mapeamento por Entity (se existir).
    /// </summary>
    public void RemoveByEntity(Entity entity)
    {
        if (_entityIdToKey.Remove(entity.Id, out var key))
            _keyToEntity.Remove(key, out _);
    }

    /// <summary>
    /// Tenta obter a Entity associada à chave (retorna true se existir).
    /// Notar: a Entity retornada contém a versão registrada — verifique Entity.Version se precisar validar staleness.
    /// </summary>
    public bool TryGetEntity(TKey key, out Entity entity)
    {
        return _keyToEntity.TryGetValue(key, out entity);
    }

    /// <summary>
    /// Tenta obter a chave a partir do entity.Id.
    /// </summary>
    public bool TryGetKeyByEntityId(int entityId, out TKey? key)
    {
        return _entityIdToKey.TryGetValue(entityId, out key);
    }

    /// <summary>
    /// Atualiza somente a Entity registrada para a chave (útil para atualizar versão).
    /// Retorna false se a chave não existir.
    /// </summary>
    public bool TryUpdateEntity(TKey key, Entity newEntity)
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

    /// <summary>
    /// Remove a entrada para a chave apenas se a versão registrada bater com expectedVersion.
    /// Retorna true se removeu.
    /// </summary>
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

    /// <summary>
    /// Remove a entrada para a entidade apenas se a versão registrada bater com expectedVersion.
    /// Retorna true se removeu.
    /// </summary>
    public bool TryRemoveByEntityIfVersionMatches(Entity entity, int expectedVersion)
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

    /// <summary>
    /// Limpa todos os mapeamentos.
    /// </summary>
    public void Clear()
    {
        _keyToEntity.Clear();
        _entityIdToKey.Clear();
    }

    /// <summary>
    /// Snapshot imutável (cópia) dos mapeamentos atuais (key -> Entity).
    /// </summary>
    public IReadOnlyDictionary<TKey, Entity> Snapshot()
    {
        return _keyToEntity.ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    /// <summary>
    /// Reconstrói o índice a partir de um enumerável de tuplas (key, entity).
    /// </summary>
    public void RebuildFrom(IEnumerable<(TKey key, Entity entity)> items)
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