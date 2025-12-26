using Game.Domain.Commons;
using Game.Domain.Enums;

namespace Game.Domain.Events;

/// <summary>
/// Evento disparado quando um item é equipado.
/// </summary>
public sealed record ItemEquippedEvent : BaseDomainEvent
{
    public int CharacterId { get; init; }
    public int ItemId { get; init; }
    public EquipmentSlotType Slot { get; init; }
}

/// <summary>
/// Evento disparado quando um item é desequipado.
/// </summary>
public sealed record ItemUnequippedEvent : BaseDomainEvent
{
    public int CharacterId { get; init; }
    public int ItemId { get; init; }
    public EquipmentSlotType Slot { get; init; }
}
