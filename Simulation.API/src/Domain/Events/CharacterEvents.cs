namespace GameWeb.Domain.Events;

/// <summary>
/// Evento de domínio base para todos os eventos relacionados a contas de usuário
/// </summary>
public abstract record CharacterEvent(int Id) : BaseEvent(Id);

public sealed record CharacterCreatedEvent(int Id) : CharacterEvent(Id);
public sealed record CharacterActivatedEvent(int Id) : CharacterEvent(Id);
public sealed record CharacterDeactivatedEvent(int Id) : CharacterEvent(Id);
public sealed record CharacterDeletedEvent(int Id) : CharacterEvent(Id);
