using System.ComponentModel.DataAnnotations.Schema;

namespace GameWeb.Domain.Common;

/// <summary>
/// Entidade base do domínio com Id, Ativo/Inativo e eventos de domínio.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; private set; } // identidade
    public bool IsActive { get; set; } = true; // ativo ou inativo
    
    private readonly List<BaseEvent> _domainEvents = []; // eventos de domínio :contentReference[oaicite:9]{index=9}
    [NotMapped]
    public IReadOnlyCollection<BaseEvent> DomainEvents => _domainEvents;

    public void AddDomainEvent(BaseEvent domainEventItem)
        => _domainEvents.Add(domainEventItem);

    public void RemoveDomainEvent(BaseEvent domainEventItem)
        => _domainEvents.Remove(domainEventItem);

    public IReadOnlyCollection<BaseEvent> GetDomainEvents()
        => _domainEvents.AsReadOnly();

    public void ClearDomainEvents()
        => _domainEvents.Clear();

    public override bool Equals(object? obj)
    {
        if (obj is not BaseEntity other) return false;
        if (ReferenceEquals(this, other)) return true;
        if (GetUnproxiedType(this) != GetUnproxiedType(other)) return false;

        // Somente Id: se ainda não foi atribuído (0), considere diferente
        if (Id == default || other.Id == default) 
            return false;

        return Id == other.Id;
    }

    public override int GetHashCode()
        => (GetUnproxiedType(this).ToString() + Id)
            .GetHashCode(); // hash baseado em Id

    internal static Type GetUnproxiedType(object obj)
    {
        var type = obj.GetType();
        var name = type.ToString();
        if (name.StartsWith("Castle.Proxies.") && type.BaseType is not null)
            return type.BaseType;
        return type;
    }
}
