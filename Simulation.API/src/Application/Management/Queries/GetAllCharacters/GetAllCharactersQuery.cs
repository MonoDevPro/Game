using Application.Models;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Common.Models;
using GameWeb.Application.Common.Security;
using GameWeb.Application.Management.Specifications;
using GameWeb.Domain.Constants;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Management.Queries.GetAllCharacters;

[Authorize(Roles = Roles.Administrator)]
public record GetAllCharactersQuery(bool? IsActive = null, int PageNumber = 1, int PageSize = 10) 
    : IQuery<PaginatedList<CharacterDto>>;

public class GetAllCharactersQueryHandler(
    IRepository<Character> characterRepo)
    : IRequestHandler<GetAllCharactersQuery, PaginatedList<CharacterDto>>
{
    public async Task<PaginatedList<CharacterDto>> Handle(GetAllCharactersQuery request, CancellationToken cancellationToken)
    {
        var spec = new AllCharactersAdminSpec(request.IsActive);
        
        return await characterRepo.ListBySpecAsync<CharacterDto>(
            spec,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}

