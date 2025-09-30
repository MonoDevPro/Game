using Application.Abstractions;
using Application.Abstractions.Commons;
using GameWeb.Application.Characters.Services;
using GameWeb.Application.Common.Security;
using GameWeb.Domain.Constants;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Management.Queries.GetAllCharacters;

// Only administrators can manage users
[Authorize(Policy = Policies.CanReadCharacters)]
public record GetAllCharactersQuery(bool? IsActive = null, int PageNumber = 1, int PageSize = 10) 
    : IQuery<IPaginatedList<Player>>;

public class GetAllCharactersQueryHandler(
    IPlayerRepository characterRepo)
    : IRequestHandler<GetAllCharactersQuery, IPaginatedList<Player>>
{
    public async Task<IPaginatedList<Player>> Handle(GetAllCharactersQuery request, CancellationToken cancellationToken)
    {
        var characters = await characterRepo.ListPagedAsync(
            request.PageNumber, request.PageSize, true, request.IsActive ?? true , cancellationToken);
        return characters;
    }
}

