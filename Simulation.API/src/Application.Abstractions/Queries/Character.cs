using Application.Models.Models;
using Application.Models.Security;
using GameWeb.Domain.Constants;

namespace Application.Models.Queries;

[Authorize(Policy = Policies.CanManageUsers)]
public record GetAllCharactersQuery(bool? IsActive = null, int PageNumber = 1, int PageSize = 10) 
    : IQuery<PaginatedList<CharacterDto>>;

public record GetMyCharactersQuery : IQuery<List<CharacterDto>>;
