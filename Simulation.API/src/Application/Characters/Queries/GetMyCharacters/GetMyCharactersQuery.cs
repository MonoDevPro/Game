using Application.Abstractions;
using GameWeb.Application.Characters.Services;
using GameWeb.Application.Common.Interfaces;

namespace GameWeb.Application.Characters.Queries.GetMyCharacters;

public record GetMyCharactersQuery : IQuery<List<PlayerData>>;

public class GetMyCharactersQueryHandler(
    IPlayerRepository characterRepo, 
    IUser user, IMapper map)
    : IRequestHandler<GetMyCharactersQuery, List<PlayerData>>
{
    public async Task<List<PlayerData>> Handle(GetMyCharactersQuery request, CancellationToken cancellationToken)
    {
        var myCharacters = await characterRepo
            .GetPlayersByUserIdAsync(user.Id!, true, false, cancellationToken);
        return myCharacters.Select(map.Map<PlayerData>).ToList();
    }
}
