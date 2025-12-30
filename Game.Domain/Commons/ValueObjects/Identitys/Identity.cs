using Game.Domain.Commons.Enums;

namespace Game.Domain.Commons.ValueObjects.Identitys;

/// <summary>
/// Identidade única da entidade.
/// </summary>
public struct Identity
{
    public int UniqueId;
    public EntityType Type;
}