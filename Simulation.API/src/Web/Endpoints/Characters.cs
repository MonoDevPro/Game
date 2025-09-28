using GameWeb.Application.Characters.Commands.CreateCharacter;
using GameWeb.Application.Characters.Commands.DeleteCharacter;
using GameWeb.Application.Characters.Queries.GetMyCharacters;
using Microsoft.AspNetCore.Mvc;

namespace GameWeb.Web.Endpoints;

public class Characters : EndpointGroupBase
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
    public async Task<IResult> CreateCharacter(ISender sender, [FromBody] CreateCharacterCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Created($"/characters/{result.Id}", result);
    }

    /// <summary>
    /// Deleta um personagem específico.
    /// </summary>
    public async Task<IResult> DeleteCharacter(ISender sender, int id)
    {
        var result = await sender.Send(new DeleteCharacterCommand(id));
        return TypedResults.Ok(result);
    }

    /// <summary>
    /// Obtém a lista de personagens do usuário logado.
    /// </summary>
    public async Task<IResult> GetMyCharacters(ISender sender)
    {
        var result = await sender.Send(new GetMyCharactersQuery());
        return TypedResults.Ok(result);
    }
}
