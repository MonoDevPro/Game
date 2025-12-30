using Game.Domain.Commons;
using Game.Domain.Enums;

namespace Game.Domain.Entities;

/// <summary>
/// Slot de equipamento persistido.
/// </summary>
public class EquipmentSlot : BaseEntity
{
    public int CharacterId { get; set; }
    public Character Character { get; set; } = null!;
    public EquipmentSlotType SlotType { get; set; }
    public int? ItemId { get; set; }
    public Item? Item { get; set; }
}
