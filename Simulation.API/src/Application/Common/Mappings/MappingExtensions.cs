using GameWeb.Application.Common.Models;

namespace GameWeb.Application.Common.Mappings;

public static class MappingExtensions
{
    public static Task<PaginatedList<TDestination>> PaginatedListAsync<TDestination>(this IQueryable<TDestination> queryable, int pageNumber, int pageSize, CancellationToken cancellationToken = default) where TDestination : class
        => PaginatedList<TDestination>.CreateAsync(queryable.AsNoTracking(), pageNumber, pageSize, cancellationToken);

    public static Task<List<TDestination>> ProjectToListAsync<TDestination>(this IQueryable queryable, IMapper mapper, CancellationToken cancellationToken = default) where TDestination : class
        => queryable.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().ToListAsync(cancellationToken);
    
    public static Task<TDestination?> ProjectToSingleOrDefaultAsync<TDestination>(this IQueryable queryable, IMapper mapper, CancellationToken cancellationToken = default) where TDestination : class
        => queryable.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().SingleOrDefaultAsync(cancellationToken);
    
    public static Task<TDestination?> ProjectToFirstOrDefaultAsync<TDestination>(this IQueryable queryable, IMapper mapper, CancellationToken cancellationToken = default) where TDestination : class
        => queryable.ProjectTo<TDestination>(mapper.ConfigurationProvider).AsNoTracking().FirstOrDefaultAsync(cancellationToken);
}
