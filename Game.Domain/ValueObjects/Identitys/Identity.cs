using Game.Domain.Enums;

namespace Game.Domain.ValueObjects.Identitys;

/// <summary>
/// Identidade única da entidade.
/// </summary>
public struct Identity
{
    public int UniqueId;
    public EntityType Type;
}