using Application.Models.Models;
using Application.Models.Queries;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Management.Specifications;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Management.Queries.GetAllCharacters;

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

