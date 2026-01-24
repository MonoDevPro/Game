using Domain.Entities;
using Domain.Entities.Common;

namespace Domain.Events;

public record CharacterResurrectedEvent(Character Character) : IDomainEvent;
