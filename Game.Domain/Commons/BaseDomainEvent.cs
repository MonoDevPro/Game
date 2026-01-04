namespace Game.Domain.Commons;

/// <summary>
/// Implementação base para eventos de domínio.
/// </summary>
public abstract record BaseDomainEvent : IDomainEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTimeOffset OccurredAt { get; init; } = DateTimeOffset.UtcNow;
}
