using Microsoft.EntityFrameworkCore;
using Server.Persistence.Context;
using Simulation.Core.Persistence;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Contracts.Commons;

namespace Server.Persistence.Repositories;

// Repositório genérico EF Core.
// Nota: por convenção este repo NÃO chama SaveChanges automaticamente em Add/Update/Remove,
// mas expõe SaveChangesAsync para o chamador controlar transações / UoW.
// Se preferir comportamento "auto-save", é só adicionar overloads que chamam SaveChangesAsync.
public class EFCoreRepository<TKey, TEntity>(SimulationDbContext context) : IRepositoryAsync<TKey, TEntity>
    where TKey : notnull
    where TEntity : class
{
    protected readonly SimulationDbContext Context = context ?? throw new ArgumentNullException(nameof(context));
    protected readonly DbSet<TEntity> DbSet = context.Set<TEntity>();

    public virtual async Task<TEntity?> GetAsync(TKey id, CancellationToken ct = default)
    {
        // FindAsync espera um array de chaves e cancellation token separado em EF Core 6+
        var entry = await DbSet.FindAsync(new object[] { id }, ct);
        return entry;
    }

    public virtual async Task<(bool Found, TEntity? Entity)> TryGetAsync(TKey id, CancellationToken ct = default)
    {
        var entity = await GetAsync(id, ct);
        return (entity != null, entity);
    }

    public virtual async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await DbSet.AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task<PagedResult<TEntity>> GetPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

        var totalCount = await DbSet.CountAsync(ct);
        var items = await DbSet
            .AsNoTracking()
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedResult<TEntity>(items, totalCount, page, pageSize);
    }

    public virtual async Task<IReadOnlyList<TEntity>> FindAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
    {
        if (spec == null) throw new ArgumentNullException(nameof(spec));
        return await ApplySpecification(spec).AsNoTracking().ToListAsync(ct);
    }

    public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        await DbSet.AddAsync(entity, ct);
        // NOTA: não chamamos SaveChangesAsync aqui por padrão
    }

    public virtual Task<bool> UpdateAsync(TKey id, TEntity entity, CancellationToken ct = default)
    {
        if (entity == null) throw new ArgumentNullException(nameof(entity));
        // marca como modificado - assume que o entity tem a key corretamente preenchida
        Context.Entry(entity).State = EntityState.Modified;
        return Task.FromResult(true);
    }

    public virtual async Task<bool> RemoveAsync(TKey id, CancellationToken ct = default)
    {
        var entity = await GetAsync(id, ct);
        if (entity == null) return false;
        DbSet.Remove(entity);
        return true;
    }

    // Componente de persistência: commit explicito
    public virtual Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        return Context.SaveChangesAsync(ct);
    }

    protected IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec)
    {
        IQueryable<TEntity> query = DbSet.AsQueryable();

        // aplicar where
        query = query.Where(spec.Criteria);

        // includes
        if (spec.Includes.Count > 0)
            query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        // order by
        if (spec.OrderBy != null)
            query = query.OrderBy(spec.OrderBy);
        else if (spec.OrderByDescending != null)
            query = query.OrderByDescending(spec.OrderByDescending);

        return query;
    }

    public virtual ValueTask DisposeAsync()
    {
        // Se o contexto for gerenciado pelo DI (Scoped), não devemos dispor aqui
        // por isso deixamos a implementação vazia; se você controla o contexto, chame _context.DisposeAsync().
        return ValueTask.CompletedTask;
    }
}
