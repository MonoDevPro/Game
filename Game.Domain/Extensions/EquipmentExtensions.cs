using Game.Domain.Entities;
using Game.Domain.Enums;

namespace Game.Domain.Extensions;

public static class EquipmentExtensions
{
    public static bool IsEquipment(this ItemType itemType)
        => itemType == ItemType.Head ||
           itemType == ItemType.Chest ||
           itemType == ItemType.Legs ||
           itemType == ItemType.Feet ||
           itemType == ItemType.Hands ||
           itemType == ItemType.MainHand ||
           itemType == ItemType.OffHand ||
           itemType == ItemType.Accessory1 ||
           itemType == ItemType.Accessory2 ||
           itemType == ItemType.Accessory3;
    
    public static bool MatchesEquipmentSlot(this ItemType itemType, EquipmentSlotType slotType)
        => (itemType == ItemType.Head && slotType == EquipmentSlotType.Head) ||
           (itemType == ItemType.Chest && slotType == EquipmentSlotType.Chest) ||
           (itemType == ItemType.Legs && slotType == EquipmentSlotType.Legs) ||
           (itemType == ItemType.Feet && slotType == EquipmentSlotType.Feet) ||
           (itemType == ItemType.Hands && slotType == EquipmentSlotType.Hands) ||
           (itemType == ItemType.MainHand && slotType == EquipmentSlotType.MainHand) ||
           (itemType == ItemType.OffHand && slotType == EquipmentSlotType.OffHand) ||
           (itemType == ItemType.Accessory1 && slotType == EquipmentSlotType.Accessory1) ||
           (itemType == ItemType.Accessory2 && slotType == EquipmentSlotType.Accessory2) ||
           (itemType == ItemType.Accessory3 && slotType == EquipmentSlotType.Accessory3);
    
        /// <summary>
    /// Verifica se o personagem pode equipar o item no slot especificado.
    /// </summary>
    public static (bool IsSatisfied, string? ErrorMessage) CanEquip(this Character character, Item item, EquipmentSlotType slot, LangType langType = LangType.PT_BR)
    {
        // Verificações necessárias:
        
        // 0. Personagem está ativo
        if (!character.IsActive)
            return (false, "Character must be active to equip items");
        
        // 1. Item existe
        if (item.Id <= 0)
            return (false, "Invalid item");
        
        // 1.1 Personagem possui o item no inventário
        if (!character.Inventory.HasItem(item.Id, 1))
            return (false, "Character does not possess the item");
        
        // 2. Item é equipável
        if (!item.Type.IsEquipment())
            return (false, "Item is not equippable");
        
        // 3. Item corresponde ao slot
        if (!item.Type.MatchesEquipmentSlot(slot))
            return (false, GameErrors.GetEquipmentSlotRequirementError((LangType)character.Account.PreferredLanguage));
        
        // 4. Personagem atende aos requisitos do item
        if (character.Level < item.RequiredLevel)
            return (false, GameErrors.GetLevelRequirementError(item.RequiredLevel, langType));
        
        if (item.RequiredVocation != VocationType.None && item.RequiredVocation != (VocationType)character.Vocation)
            return (false, GameErrors.GetVocationRequirementError(item.RequiredVocation, langType));
        
        if (character.Strength < item.RequiredStats.Strength)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Strength, langType));
        
        if (character.Dexterity < item.RequiredStats.Dexterity)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Dexterity, langType));
        
        if (character.Intelligence < item.RequiredStats.Intelligence)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Intelligence, langType));
        
        if (character.Constitution < item.RequiredStats.Constitution)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Constitution, langType));
        
        if (character.Spirit < item.RequiredStats.Spirit)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Spirit, langType));
        
        return (true, null);
    }
}