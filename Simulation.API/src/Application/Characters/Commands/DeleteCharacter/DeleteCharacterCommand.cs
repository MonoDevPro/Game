using GameWeb.Application.Characters.Specifications;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;
using GameWeb.Domain.Events;

namespace GameWeb.Application.Characters.Commands.DeleteCharacter;

public record DeleteCharacterCommand(int CharacterId) : ICommand<int>;

public class DeleteCharacterCommandHandler(
    IRepository<Character> characterRepo, 
    IUser user)
    : IRequestHandler<DeleteCharacterCommand, int>
{
    public async Task<int> Handle(DeleteCharacterCommand request, CancellationToken cancellationToken)
    {
        var spec = new DeletableCharacterSpec(request.CharacterId, user);
        
        var character = Guard.Against.Null(await characterRepo.GetBySpecAsync(spec, cancellationToken), nameof(Character), 
            $"Character with ID {request.CharacterId} not found or you don't have permission to delete it.", () => new NotFoundException(request.CharacterId.ToString(), nameof(Character)));
        
        character.IsActive = false;
        character.AddDomainEvent(new CharacterDeactivatedEvent(character.Id));
        characterRepo.Update(character);
        
        return character.Id;
    }
}
