using Application.Abstractions;
using GameWeb.Application.Characters.Services;
using GameWeb.Application.Common.Security;
using GameWeb.Domain.Constants;
using GameWeb.Domain.Events;

namespace GameWeb.Application.Management.Commands.PurgeCharacters;

// Only administrators can purge characters
[Authorize(Policy = Policies.CanDeleteCharacters)]
public record PurgeCharactersCommand(bool? IsActive = null) : ICommand<long>;


public class PurgeCharactersCommandHandler(IPlayerRepository characterRepo)
    : IRequestHandler<PurgeCharactersCommand, long>
{
    public async Task<long> Handle(PurgeCharactersCommand request, CancellationToken cancellationToken)
    {
        // 1. Usa a especificação para obter a lista de personagens a serem removidos.
        var characters = await characterRepo.ListPagedAsync(
            1, int.MaxValue, true, request.IsActive ?? false , cancellationToken);

        if (characters.TotalCount == 0)
            return 0; // Nenhum personagem para remover.
        
        // 2. Itera sobre a lista e marca cada um para ser apagado.
        foreach (var character in characters.Items)
        {
            character.AddDomainEvent(new CharacterDeletedEvent(character.Id));
            characterRepo.Delete(character);
        }

        return characters.TotalCount;
    }
}
