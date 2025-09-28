using Application.Models;
using GameWeb.Application.Characters.Specifications;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Queries.GetMyCharacters;

public record GetMyCharactersQuery : IQuery<List<CharacterDto>>;

public class GetMyCharactersQueryHandler(
    IRepository<Character> characterRepo, 
    IUser user)
    : IRequestHandler<GetMyCharactersQuery, List<CharacterDto>>
{
    public async Task<List<CharacterDto>> Handle(GetMyCharactersQuery request, CancellationToken cancellationToken)
    {
        var spec = new CharactersByOwnerSpec(user.Id!);
        return await characterRepo.ListBySpecAsync<CharacterDto>(spec, cancellationToken);
    }
}
