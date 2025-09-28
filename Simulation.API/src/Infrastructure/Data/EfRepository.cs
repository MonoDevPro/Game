using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Common.Models;
using GameWeb.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace GameWeb.Infrastructure.Data;

public class EfRepository<T>(ApplicationDbContext context, IMapper mapper) : IRepository<T>
    where T : BaseEntity
{
    // --- Helpers ---------------------------------------------------------

    // Query leve usado para COUNT (sem includes/orderings)
    private IQueryable<T> ApplySpecificationForCount(ISpecification<T> spec)
    {
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        var query = context.Set<T>().AsQueryable();

        if (spec.IgnoreQueryFilters) query = query.IgnoreQueryFilters();
        if (spec.Criteria != null) query = query.Where(spec.Criteria);
        if (spec.AsNoTracking) query = query.AsNoTracking();

        return query;
    }

    // Query completa usada para buscar os itens (includes, orderings, paging opcional)
    private IQueryable<T> ApplySpecificationForQuery(ISpecification<T> spec)
    {
        if (spec == null) throw new ArgumentNullException(nameof(spec));

        var query = context.Set<T>().AsQueryable();

        if (spec.AsNoTracking) query = query.AsNoTracking();
        if (spec.IgnoreQueryFilters) query = query.IgnoreQueryFilters();

        if (spec.Criteria != null) query = query.Where(spec.Criteria);

        // includes por expressão
        foreach (var include in spec.Includes)
            query = query.Include(include);

        // includes por string (se suportado)
        foreach (var includeString in spec.IncludeStrings)
            query = query.Include(includeString);

        // orderings (múltiplos)
        bool firstOrdering = true;
        foreach (var (keySelector, descending) in spec.OrderBy)
        {
            if (firstOrdering)
            {
                query = descending ? query.OrderByDescending(keySelector) : query.OrderBy(keySelector);
                firstOrdering = false;
            }
            else
            {
                query = descending ? ((IOrderedQueryable<T>)query).ThenByDescending(keySelector)
                                   : ((IOrderedQueryable<T>)query).ThenBy(keySelector);
            }
        }

        // paging se a spec definiu
        if (spec.IsPagingEnabled && spec.Skip.HasValue && spec.Take.HasValue)
            query = query.Skip(spec.Skip.Value).Take(spec.Take.Value);

        return query;
    }

    // Utility: valida paginação (pageNumber/pageSize)
    private static void ValidatePagingParams(int pageNumber, int pageSize)
    {
        if (pageNumber < 1) throw new ArgumentOutOfRangeException(nameof(pageNumber), "pageNumber must be >= 1");
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize), "pageSize must be > 0");
    }

    // --- CRUD-like/query methods -----------------------------------------

    public async Task<T?> GetBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecificationForQuery(spec);
        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TResult?> GetBySpecAsync<TResult>(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        // Use full query so includes/orderings from spec are aplicadas (se relevantes)
        var query = ApplySpecificationForQuery(spec);

        // respeitar AsNoTracking da spec
        if (spec.AsNoTracking) query = query.AsNoTracking();

        if (spec.Selector is Expression<Func<T, TResult>> sel)
        {
            return await query.Select(sel).FirstOrDefaultAsync(cancellationToken);
        }

        return await query.ProjectTo<TResult>(mapper.ConfigurationProvider).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<T>> ListBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecificationForQuery(spec);
        return await query.ToListAsync(cancellationToken);
    }

    public async Task<List<TResult>> ListBySpecAsync<TResult>(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        var query = ApplySpecificationForQuery(spec);

        IQueryable<TResult> projected;
        if (spec.Selector is Expression<Func<T, TResult>> sel)
            projected = query.Select(sel);
        else
            projected = query.ProjectTo<TResult>(mapper.ConfigurationProvider);

        return await projected.ToListAsync(cancellationToken);
    }
    
    public async Task<bool> AnyBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecificationForCount(spec).AnyAsync(cancellationToken);
    }

    public async Task<int> CountBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default)
    {
        return await ApplySpecificationForCount(spec).CountAsync(cancellationToken: cancellationToken);
    }

    // --- Paginated methods -----------------------------------------------

    public async Task<PaginatedList<T>> ListBySpecAsync(ISpecification<T> spec, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePagingParams(pageNumber, pageSize);

        // Se não há OrderBy definido e vamos aplicar paginação, avisar/lançar — paginação sem order é não determinística
        if (!spec.OrderBy.Any())
            throw new InvalidOperationException("Paging requested but no OrderBy specified in specification. Provide an OrderBy to ensure deterministic paging.");

        var countQuery = ApplySpecificationForCount(spec);
        var totalCount = await countQuery.CountAsync(cancellationToken);

        // A paginação é aplicada aqui, usando os parâmetros do método, não da especificação.
        var itemsQuery = ApplySpecificationForQuery(spec)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize);
        
        var items = await itemsQuery.ToListAsync(cancellationToken);

        return new PaginatedList<T>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<PaginatedList<TResult>> ListBySpecAsync<TResult>(ISpecification<T> spec, int pageNumber, int pageSize, CancellationToken cancellationToken = default)
    {
        ValidatePagingParams(pageNumber, pageSize);

        if (!spec.OrderBy.Any())
            throw new InvalidOperationException("Paging requested but no OrderBy specified in specification. Provide an OrderBy to ensure deterministic paging.");

        var countQuery = ApplySpecificationForCount(spec);
        var totalCount = await countQuery.CountAsync(cancellationToken);

        var itemsQuery = ApplySpecificationForQuery(spec);

        IQueryable<TResult> projected;
        if (spec.Selector is Expression<Func<T, TResult>> sel)
            projected = itemsQuery.Select(sel);
        else
            projected = itemsQuery.ProjectTo<TResult>(mapper.ConfigurationProvider);

        if (!spec.IsPagingEnabled)
            projected = projected.Skip((pageNumber - 1) * pageSize).Take(pageSize);

        var items = await projected.ToListAsync(cancellationToken);

        var usedPageNumber = spec.IsPagingEnabled ? (spec.Skip.HasValue && spec.Take.HasValue ? (spec.Skip.Value / spec.Take.Value) + 1 : pageNumber) : pageNumber;
        var usedPageSize   = spec.IsPagingEnabled ? (spec.Take ?? pageSize) : pageSize;

        return new PaginatedList<TResult>(items, totalCount, usedPageNumber, usedPageSize);
    }
    
    public void Add(T entity)
    {
        context.Set<T>().Add(entity);
    }

    public void Update(T entity)
    {
        context.Set<T>().Update(entity);
    }

    public void Delete(T entity)
    {
        context.Set<T>().Remove(entity);
    }
}
