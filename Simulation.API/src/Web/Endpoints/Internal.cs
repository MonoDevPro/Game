using GameWeb.Application.Management.Commands.PurgeCharacters;
using GameWeb.Application.Management.Queries.GetAllCharacters;
using GameWeb.Domain.Constants;

namespace GameWeb.Web.Endpoints;

public class Internal : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization(Policies.InternalOnly);

        var charactersGroup = group.MapGroup("/internal");
        
        charactersGroup.MapGet(GetSelectedCharacter, "/{id}")
            .WithSummary("Get a character selected and details by ID (Internal only).");
    }

    public async Task<IResult> GetSelectedCharacter(ISender sender, [AsParameters] GetAllCharactersQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<IResult> PurgeCharacters(ISender sender, bool? isActive = null)
    {
        var command = new PurgeCharactersCommand(isActive);
        var result = await sender.Send(command);
        return TypedResults.Ok(new { PurgedCount = result });
    }
}
