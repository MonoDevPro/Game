using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Common.Mapping;
using GameWeb.Application.Players.Models;

namespace GameWeb.Application.Players.Queries.GetMyPlayers;

public record GetMyPlayersQuery : IQuery<List<PlayerDto>>;

public class GetMyPlayersQueryHandler(
    IApplicationDbContext context, 
    IUser user, IMapper map)
    : IRequestHandler<GetMyPlayersQuery, List<PlayerDto>>
{
    public async Task<List<PlayerDto>> Handle(GetMyPlayersQuery request, CancellationToken cancellationToken)
    {
        return await context.Players
            .Where(p => p.UserId == user.Id && p.IsActive)
            .ProjectToListAsync<PlayerDto>(map, cancellationToken);
    }
}
