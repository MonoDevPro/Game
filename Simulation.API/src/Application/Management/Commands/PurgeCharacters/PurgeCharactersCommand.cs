using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Common.Security;
using GameWeb.Application.Management.Specifications;
using GameWeb.Domain.Constants;
using GameWeb.Domain.Entities;
using GameWeb.Domain.Events;

namespace GameWeb.Application.Management.Commands.PurgeCharacters;

[Authorize(Policy = Policies.CanPurge)]
public record PurgeCharactersCommand(bool? IsActive = null) : ICommand<int>;

public class PurgeCharactersCommandHandler(IRepository<Character> characterRepo)
    : IRequestHandler<PurgeCharactersCommand, int>
{
    public async Task<int> Handle(PurgeCharactersCommand request, CancellationToken cancellationToken)
    {
        // 1. Usa a especificação para obter a lista de personagens a serem removidos.
        var spec = new CharactersForPurgeSpec(request.IsActive);
        var characters = await characterRepo.ListBySpecAsync(spec, cancellationToken);

        if (characters.Count == 0)
            return 0; // Nenhum personagem para remover.
        
        // 2. Itera sobre a lista e marca cada um para ser apagado.
        foreach (var character in characters)
        {
            character.AddDomainEvent(new CharacterDeletedEvent(character.Id));
            characterRepo.Delete(character);
        }

        return characters.Count;
    }
}
