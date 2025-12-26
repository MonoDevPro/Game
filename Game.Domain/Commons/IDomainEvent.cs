namespace Game.Domain.Commons;

/// <summary>
/// Interface base para todos os eventos de domínio.
/// Domain Events representam fatos que ocorreram no domínio.
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Identificador único do evento.
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// Timestamp de quando o evento ocorreu.
    /// </summary>
    DateTimeOffset OccurredAt { get; }
}
