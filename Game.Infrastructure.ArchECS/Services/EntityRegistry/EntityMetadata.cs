using Arch.Core;

namespace Game.Infrastructure.ArchECS.Services.EntityRegistry;

/// <summary>
/// Metadados de uma entidade registrada.
/// Struct para performance (evita heap allocations).
/// </summary>
public struct EntityMetadata
{
    public int ExternalId;
    public Entity Entity;
    public EntityDomain Domain;
    public DateTime RegisteredAt;

    public override string ToString()
    {
        return $"Entity[{ExternalId}] - Domains: {Domain}";
    }
}