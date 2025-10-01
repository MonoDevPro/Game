using GameWeb.Application.Players.Commands.Create;
using GameWeb.Application.Players.Commands.Deactivate;
using GameWeb.Application.Players.Queries.GetMyPlayers;
using Microsoft.AspNetCore.Mvc;

namespace GameWeb.Web.Endpoints;

public class Players : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.RequireAuthorization();

        // O método Map agora é apenas um "índice" de rotas, muito mais limpo
        group.MapPost(CreateCharacter)
            .WithSummary("Create a new character for the logged-in user.");
        
        group.MapGet(GetMyCharacters, "/me")
            .WithSummary("Get the list of characters for the logged-in user.");
        
        group.MapDelete(DeleteCharacter, "{id}")
            .WithSummary("Delete a specific character.");
    }

    // --- MÉTODOS DE HANDLER PARA CADA ENDPOINT ---

    /// <summary>
    /// Cria um novo personagem para o usuário logado.
    /// </summary>
    public async Task<IResult> CreateCharacter(ISender sender, [FromBody] CreatePlayerCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Created($"/characters/{result.Id}", result);
    }

    /// <summary>
    /// Deleta um personagem específico.
    /// </summary>
    public async Task<IResult> DeleteCharacter(ISender sender, int id)
    {
        var result = await sender.Send(new DeactivatePlayerCommand(id));
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Obtém a lista de personagens do usuário logado.
    /// </summary>
    public async Task<IResult> GetMyCharacters(ISender sender)
    {
        var result = await sender.Send(new GetMyPlayersQuery());
        return TypedResults.Ok(result);
    }
}
