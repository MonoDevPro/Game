using Arch.Core;

namespace Game.ECS.Services;

public interface IEntityIndex<TKey> where TKey : notnull
{
    /// Número de mapeamentos atuais.
    int Count { get; }
    
    /// <summary>
    /// Adiciona ou atualiza o mapeamento key -> entity.
    /// Substitui a entrada anterior, removendo o reverse mapping antigo se necessário.
    /// </summary>
    void Register(TKey key, Entity entity);

    /// <summary>
    /// Tenta adicionar apenas se a chave ainda não existir. Retorna true se adicionou.
    /// </summary>
    bool TryRegisterUnique(TKey key, Entity entity);

    /// <summary>
    /// Remove mapeamento por chave (se existir).
    /// </summary>
    void RemoveByKey(TKey key);

    /// <summary>
    /// Remove mapeamento por Entity (se existir).
    /// </summary>
    void RemoveByEntity(Entity entity);
    
    /// <summary>
    /// Tenta obter a Entity associada à chave (retorna true se existir).
    /// Notar: a Entity retornada contém a versão registrada — verifique Entity.Version se precisar validar staleness.
    /// </summary>
    bool TryGetEntity(TKey key, out Entity entity);

    /// <summary>
    /// Tenta obter a chave a partir do entity.Id.
    /// </summary>
    bool TryGetKeyByEntityId(int entityId, out TKey? key);

    /// <summary>
    /// Tenta obter a chave a partir da Entity.
    /// </summary>
    bool TryGetKeyByEntity(Entity entity, out TKey? key);

    /// <summary>
    /// Atualiza somente a Entity registrada para a chave (útil para atualizar versão).
    /// Retorna false se a chave não existir.
    /// </summary>
    bool TryUpdateEntity(TKey key, Entity newEntity);

    /// <summary>
    /// Remove a entrada para a chave apenas se a versão registrada bater com expectedVersion.
    /// Retorna true se removeu.
    /// </summary>
    bool TryRemoveByKeyIfVersionMatches(TKey key, int expectedVersion);

    /// <summary>
    /// Remove a entrada para a entidade apenas se a versão registrada bater com expectedVersion.
    /// Retorna true se removeu.
    /// </summary>
    bool TryRemoveByEntityIfVersionMatches(Entity entity, int expectedVersion);

    /// <summary>
    /// Limpa todos os mapeamentos.
    /// </summary>
    void Clear();

    /// <summary>
    /// Snapshot imutável (cópia) dos mapeamentos atuais (key -> Entity).
    /// </summary>
    IReadOnlyDictionary<TKey, Entity> Snapshot();

    /// <summary>
    /// Reconstrói o índice a partir de um enumerável de tuplas (key, entity).
    /// </summary>
    void RebuildFrom(IEnumerable<(TKey key, Entity entity)> items);
}