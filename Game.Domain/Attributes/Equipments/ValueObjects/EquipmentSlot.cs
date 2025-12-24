using Game.Domain.Commons.Enums;

namespace Game.Domain.Attributes.Equipments.ValueObjects;

public readonly struct EquipmentSlot(EquipmentSlotType slotType, int itemId = 0)
{
    public EquipmentSlotType SlotType { get; } = slotType;
    public int ItemId { get; } = itemId;

    public bool IsEmpty => ItemId == 0;

    public EquipmentSlot WithItem(int itemId) => new(SlotType, itemId);
    public static EquipmentSlot Empty(EquipmentSlotType slotType) => new(slotType, 0);
}
