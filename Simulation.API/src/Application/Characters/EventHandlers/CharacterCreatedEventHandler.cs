using GameWeb.Domain.Events;
using Microsoft.Extensions.Logging;

namespace GameWeb.Application.Characters.EventHandlers;

public class CharacterCreatedEventHandler(ILogger<CharacterCreatedEventHandler> logger)
    : INotificationHandler<CharacterCreatedEvent>
{
    public Task Handle(CharacterCreatedEvent notification, CancellationToken cancellationToken)
    {
        logger.LogInformation("ServerGame Domain Event: {DomainEvent}", notification.GetType().Name);

        return Task.CompletedTask;
    }
}
