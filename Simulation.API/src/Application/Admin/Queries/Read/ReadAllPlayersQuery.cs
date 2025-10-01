using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Common.Mapping;
using GameWeb.Application.Common.Security;
using GameWeb.Domain.Constants;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Admin.Queries.Read;

// Only administrators can manage users
[Authorize(Policy = Policies.CanReadPlayers)]
public record ReadAllPlayersQuery(bool? IsActive = null, int PageNumber = 1, int PageSize = 10) 
    : Common.Interfaces.IQuery<IPaginatedList<Player>>;

public class ReadAllPlayersQueryHandler(
    IApplicationDbContext context)
    : IRequestHandler<ReadAllPlayersQuery, IPaginatedList<Player>>
{
    public async Task<IPaginatedList<Player>> Handle(ReadAllPlayersQuery request, CancellationToken cancellationToken)
    {
        var characters = await context.Players
            .IgnoreQueryFilters()
            .Where(p => request.IsActive == null || p.IsActive == request.IsActive)
            .OrderBy(p => p.Id)
            .PaginatedListAsync(request.PageNumber, request.PageSize, cancellationToken);
        return characters;
    }
}

