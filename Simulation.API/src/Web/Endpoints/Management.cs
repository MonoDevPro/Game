using GameWeb.Application.Admin.Commands.Purge;
using GameWeb.Application.Admin.Queries.Read;
using GameWeb.Domain.Constants;

namespace GameWeb.Web.Endpoints;

public class Management : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        // Define um prefixo e segurança para TODO o grupo administrativo
        group.RequireAuthorization(Policies.CanReadPlayers); // Opcional, pode ser na camada do CQRS

        // Cria um subgrupo para manter as rotas de personagens organizadas
        var charactersGroup = group.MapGroup("/characters");
        
        charactersGroup.MapGet(GetAllCharacters, "/all")
            .WithSummary("Get a paginated list of all characters (Admin only).");
        charactersGroup.MapDelete(PurgeCharacters, "/purge")
            .WithSummary("Purge characters based on activity status (Admin only).");
    }

    public async Task<IResult> GetAllCharacters(ISender sender, [AsParameters] ReadAllPlayersQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<IResult> PurgeCharacters(ISender sender, bool? isActive = null)
    {
        var command = new PurgeInactivePlayersCommand(isActive);
        var result = await sender.Send(command);
        return TypedResults.Ok(new { PurgedCount = result });
    }
}
