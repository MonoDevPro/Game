using GameWeb.Application.Admin.Commands.Purge;
using GameWeb.Application.Admin.Queries.Read;
using GameWeb.Domain.Constants;

namespace GameWeb.Web.Endpoints;

public class Admin : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        var charactersGroup = group.MapGroup("/characters");
        
        charactersGroup.RequireAuthorization(Policies.CanReadPlayers);
        charactersGroup.MapGet(GetAllPlayers, "/all")
            .WithSummary("Get a paginated list of all characters (Admin only).");
        
        charactersGroup.RequireAuthorization(Policies.CanDeletePlayers);
        charactersGroup.MapDelete(PurgeInactivePlayers, "/purge")
            .WithSummary("Purge characters based on activity status (Admin only).");
    }

    public async Task<IResult> GetAllPlayers(ISender sender, [AsParameters] ReadAllPlayersQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<IResult> PurgeInactivePlayers(ISender sender, bool? isActive = null)
    {
        var command = new PurgeInactivePlayersCommand(isActive);
        var result = await sender.Send(command);
        return TypedResults.Ok(new { PurgedCount = result });
    }
}
