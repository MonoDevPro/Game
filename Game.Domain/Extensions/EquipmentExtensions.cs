using Game.Domain.Enums;

namespace Game.Domain.Extensions;

public static class EquipmentExtensions
{
    public static bool IsEquipment(this ItemType itemType)
        => itemType == ItemType.Equipment;
    
    public static bool MatchesEquipmentSlot(this ItemType itemType, EquipmentSlotType slotType)
        => itemType == ItemType.Equipment && slotType != EquipmentSlotType.None;
}