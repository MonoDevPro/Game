using Microsoft.EntityFrameworkCore;
using Simulation.Core.Server.Persistence;
using Simulation.Core.Server.Persistence.Contracts;

namespace Server.Persistence.Repositories;

public class EFCoreRepository<TKey, TEntity>(DbContext context)
    : IRepositoryAsync<TKey, TEntity> // Implementa apenas Async
    where TKey : notnull
    where TEntity : class // EF Core exige que entidades sejam classes
{
    public async Task<TEntity?> GetAsync(TKey id, CancellationToken ct = default)
    {
        // FindAsync é otimizado para buscar pela chave primária.
        return await context.Set<TEntity>().FindAsync([id], cancellationToken: ct);
    }

    /// <summary>
    /// Tenta obter uma entidade pela sua chave primária de forma assíncrona.
    /// </summary>
    /// <returns>Uma tupla contendo um booleano 'Found' e a entidade encontrada ou nula.</returns>
    public async Task<(bool Found, TEntity? Entity)> TryGetAsync(TKey id, CancellationToken ct = default)
    {
        // FindAsync é o método mais otimizado para buscar uma entidade pela sua chave primária.
        // Ele primeiro verifica o cache do DbContext antes de consultar o banco de dados.
        // É necessário encapsular o 'id' em um array de object para o método.
        var entity = await context.Set<TEntity>().FindAsync([id], cancellationToken: ct);

        // Retorna uma tupla indicando se a entidade foi encontrada e a própria entidade.
        return (entity != null, entity);
    }

    public async Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return await context.Set<TEntity>().AsNoTracking().ToListAsync(ct);
    }

    /// <summary>
    /// Obtém uma página de resultados de forma assíncrona.
    /// </summary>
    public async Task<PagedResult<TEntity>> GetPageAsync(int page, int pageSize, CancellationToken ct = default)
    {
        // Validação dos parâmetros de paginação.
        if (page <= 0)
            throw new ArgumentOutOfRangeException(nameof(page), "O número da página deve ser maior que zero.");

        if (pageSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(pageSize), "O tamanho da página deve ser maior que zero.");

        // Executa uma consulta para obter a contagem total de itens no banco de dados.
        var totalCount = await context.Set<TEntity>().CountAsync(ct);

        // Executa uma segunda consulta para buscar apenas os itens da página solicitada.
        var items = await context.Set<TEntity>()
            .AsNoTracking()
            .Skip((page - 1) * pageSize) // Pula os registros das páginas anteriores.
            .Take(pageSize) // Pega a quantidade de registros para a página atual.
            .ToListAsync(ct);

        // Retorna o objeto PagedResult com os dados da paginação.
        return new PagedResult<TEntity>(items, totalCount, page, pageSize);
    }

    public async Task<IReadOnlyList<TEntity>> FindAsync(ISpecification<TEntity> spec, CancellationToken ct = default)
    {
        return await ApplySpecification(spec).ToListAsync(ct);
    }

    public async Task AddAsync(TKey id, TEntity entity, CancellationToken ct = default)
    {
        await context.Set<TEntity>().AddAsync(entity, ct);
    }

    public Task<bool> UpdateAsync(TKey id, TEntity entity, CancellationToken ct = default)
    {
        // O EF Core rastreia a entidade. O Unit of Work chamará SaveChanges.
        context.Entry(entity).State = EntityState.Modified;
        return Task.FromResult(true);
    }

    public async Task<bool> RemoveAsync(TKey id, CancellationToken ct = default)
    {
        var entity = await GetAsync(id, ct);
        if (entity != null)
        {
            context.Set<TEntity>().Remove(entity);
            return true;
        }
        return false;
    }
    
    // Métodos não implementados para brevidade (TryGetAsync, GetPageAsync, Sync, etc.)
    // ...

    private IQueryable<TEntity> ApplySpecification(ISpecification<TEntity> spec)
    {
        var query = context.Set<TEntity>().AsQueryable();

        // Aplica o critério de filtro (WHERE)
        query = query.Where(spec.Criteria);

        // Aplica os includes
        query = spec.Includes.Aggregate(query, (current, include) => current.Include(include));

        // Aplica a ordenação
        if (spec.OrderBy != null)
        {
            query = query.OrderBy(spec.OrderBy);
        }
        else if (spec.OrderByDescending != null)
        {
            query = query.OrderByDescending(spec.OrderByDescending);
        }

        return query;
    }

    public async ValueTask DisposeAsync()
    {
        await context.DisposeAsync();
    }
}