using System.Collections.Concurrent;
using Simulation.Core.Server.Persistence;
using Simulation.Core.Server.Persistence.Contracts;

namespace Server.Persistence.Repositories;

/// <summary>
/// Implementação padrão, em memória, thread-safe de um repositório/índice genérico.
/// - Usa ConcurrentDictionary internamente.
/// - Suporta reverse-lookup opcional (mantém mapa value -> key).
/// - Expõe operações sync + async (Task wrappers).
/// </summary>
public abstract class InMemoryRepository<TKey, TEntity> : IRepository<TKey, TEntity>
    where TKey : notnull
    where TEntity : class
{
    private readonly ConcurrentDictionary<TKey, TEntity> _store;

    /// <summary>
    /// Cria um DefaultRepository.
    /// </summary>
    /// <param name="enableReverseLookup">
    /// Se true, mantém um mapa reverso TEntity -> TKey. Requer que TEntity tenha Equals/GetHashCode estáveis.
    /// </param>
    /// <param name="keyComparer">Optional comparer para chaves.</param>
    /// <param name="entityComparer">Optional comparer para entidades (usado apenas se enableReverseLookup=true).</param>
    public InMemoryRepository(
        IEqualityComparer<TKey>? keyComparer = null)
    {
        _store = keyComparer is null ? new ConcurrentDictionary<TKey, TEntity>() : new ConcurrentDictionary<TKey, TEntity>(keyComparer);
    }

    #region Sync IRead / Write (IRepository)

    public TEntity? Get(TKey id)
    {
        _store.TryGetValue(id, out var value);
        return value;
    }

    public bool TryGet(TKey id, out TEntity? entity) => _store.TryGetValue(id, out entity);

    public IEnumerable<TEntity> GetAll() => _store.Values.ToArray(); // snapshot

    public PagedResult<TEntity> GetPage(int page, int pageSize)
    {
        if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var values = _store.Values;
        var total = values.Count;
        var skip = (page - 1) * pageSize;
        var items = values.Skip(skip).Take(pageSize).ToList();
        return new PagedResult<TEntity>(items, total, page, pageSize);
    }

    public IEnumerable<TEntity> Find(ISpecification<TEntity> spec)
    {
        if (spec is null) throw new ArgumentNullException(nameof(spec));
        
        // .Compile() é usado aqui porque estamos executando sobre uma coleção em memória (LINQ to Objects).
        return _store.Values.AsEnumerable().Where(spec.Criteria.Compile());
    }

    public void Add(TKey id, TEntity entity)
    {
        if (!_store.TryAdd(id, entity))
            throw new InvalidOperationException($"An item with the same key already exists: {id}");
    }

    /// <summary>
    /// Atualiza somente se a chave existir. Retorna true se a atualização ocorreu.
    /// </summary>
    public bool Update(TKey id, TEntity entity)
    {
        while (true)
        {
            if (!_store.TryGetValue(id, out var existing))
                return false;

            // Tenta atualizar atomically
            if (_store.TryUpdate(id, entity, existing))
                return true;
        }
    }

    public bool Remove(TKey id)
    {
        return _store.TryRemove(id, out var removed);
    }

    #endregion

    #region Async wrappers (IRepositoryAsync)

    public Task<TEntity?> GetAsync(TKey id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(Get(id));
    }

    public Task<(bool Found, TEntity? Entity)> TryGetAsync(TKey id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var found = _store.TryGetValue(id, out var v);
        return Task.FromResult((found, v));
    }

    public Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        IReadOnlyList<TEntity> snapshot = _store.Values.ToArray();
        return Task.FromResult(snapshot);
    }

    public Task<PagedResult<TEntity>> GetPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = GetPage(page, pageSize);
        return Task.FromResult(result);
    }

    public Task<IReadOnlyList<TEntity>> FindAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var items = Find(spec).ToList().AsReadOnly();
        return Task.FromResult((IReadOnlyList<TEntity>)items);
    }

    public Task AddAsync(TKey id, TEntity entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        Add(id, entity);
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAsync(TKey id, TEntity entity, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var r = Update(id, entity);
        return Task.FromResult(r);
    }

    public Task<bool> RemoveAsync(TKey id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var r = Remove(id);
        return Task.FromResult(r);
    }

    #endregion

    #region Helpers / Conveniences

    /// <summary>
    /// Upsert: adiciona se não existir, caso contrário substitui.
    /// Mantido como método de conveniência (não faz parte da interface).
    /// </summary>
    public void Upsert(TKey id, TEntity entity)
    {
        _store.AddOrUpdate(id, entity, (_, __) => entity);
    }

    #endregion

    public void Dispose()
    {
        // nada a liberar
    }
}