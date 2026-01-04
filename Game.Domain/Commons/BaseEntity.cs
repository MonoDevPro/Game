namespace Game.Domain.Commons;

/// <summary>
/// Entidade base do domínio com Id, Ativo/Inativo e eventos de domínio.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = new();
    
    public int Id { get; set; } // Identificador único da entidade
    
    public bool IsActive { get; set; } = true; // ativo ou inativo
    
    public DateTimeOffset CreatedAt { get; set; } 
    
    public DateTimeOffset LastUpdatedAt { get; set; }
    
    /// <summary>
    /// Eventos de domínio pendentes desta entidade.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
    
    /// <summary>
    /// Adiciona um evento de domínio à lista de eventos pendentes.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }
    
    /// <summary>
    /// Remove um evento de domínio específico.
    /// </summary>
    protected void RemoveDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Remove(domainEvent);
    }
    
    /// <summary>
    /// Limpa todos os eventos de domínio pendentes.
    /// Deve ser chamado após processar os eventos.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
    
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
