namespace Simulation.Core.Server.Persistence.Contracts;

public interface IRepositoryAsync<in TKey, TEntity>  : IAsyncDisposable
    where TKey : notnull
    where TEntity : class
{
    // Read
    Task<TEntity?> GetAsync(TKey id, CancellationToken ct = default);
    Task<(bool Found, TEntity? Entity)> TryGetAsync(TKey id, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default);
    Task<PagedResult<TEntity>> GetPageAsync(int page, int pageSize, CancellationToken ct = default);
    Task<IReadOnlyList<TEntity>> FindAsync(ISpecification<TEntity> spec, CancellationToken ct = default);
    
    // Write
    Task AddAsync(TKey id, TEntity entity, CancellationToken ct = default);
    Task<bool> UpdateAsync(TKey id, TEntity entity, CancellationToken ct = default);
    Task<bool> RemoveAsync(TKey id, CancellationToken ct = default);
}