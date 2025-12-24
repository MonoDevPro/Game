namespace Game.Domain.Commons;

/// <summary>
/// Marker interface para identificar Aggregate Roots no DDD.
/// Aggregate Roots são as entidades principais que controlam o acesso aos seus agregados.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// ID único da entidade raiz do agregado.
    /// </summary>
    int Id { get; }
}
