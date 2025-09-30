using Application.Abstractions;
using GameWeb.Application.Characters.Services;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Entities;
using GameWeb.Domain.Events;

namespace GameWeb.Application.Characters.Commands.Delete;

public record DeleteCharacterCommand(int Id) : ICommand<int>;

public class DeleteCharacterCommandHandler(
    IPlayerRepository characterRepo, 
    IUser user)
    : IRequestHandler<DeleteCharacterCommand, int>
{
    public async Task<int> Handle(DeleteCharacterCommand request, CancellationToken cancellationToken)
    {
        var character = await characterRepo.GetPlayerAsync(request.Id, true, false, cancellationToken);
        if (character == null || character.UserId != user.Id) 
            throw new NotFoundException(nameof(Player), request.Id.ToString());
        
        characterRepo.Deactivate(character);
        character.AddDomainEvent(new CharacterDeactivatedEvent(character.Id));
        
        return character.Id;
    }
}
