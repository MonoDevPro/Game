using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.Entities.Components;

/// <summary>
/// Metadados de uma entidade registrada.
/// Struct para performance (evita heap allocations).
/// </summary>
public struct EntityMetadata
{
    public int ExternalId;
    public string Name;
    public Entity Entity;
    public EntityDomain Domain;
    public DateTime RegisteredAt;

    public override string ToString()
    {
        return $"Entity[{ExternalId}] - Name:{Name} Domains: {Domain}";
    }
}