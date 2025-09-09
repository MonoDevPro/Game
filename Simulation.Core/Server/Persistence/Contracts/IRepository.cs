namespace Simulation.Core.Server.Persistence.Contracts;

public interface IRepository<in TKey, TEntity> : IDisposable
    where TKey : notnull
    where TEntity : class
{
    // Read
    TEntity? Get(TKey id);                // retorna null se não existir
    bool TryGet(TKey id, out TEntity? e); // Try pattern quando preferir evitar exceções
    IEnumerable<TEntity> GetAll();
    PagedResult<TEntity> GetPage(int page, int pageSize);
    IEnumerable<TEntity> Find(ISpecification<TEntity> spec);
    
    // Write
    void Add(TKey id, TEntity entity);   // lança ao duplicar (ou escolha Upsert)
    bool Update(TKey id, TEntity entity); // bool para sucesso/falha
    bool Remove(TKey id);
}