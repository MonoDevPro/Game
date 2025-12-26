using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.Extensions;

namespace Game.Domain.Specifications.Character;

/// <summary>
/// Especificação para validar se um personagem pode equipar um item.
/// </summary>
public class CanEquipItemSpecification
{
    /// <summary>
    /// Verifica se o personagem pode equipar o item no slot especificado.
    /// </summary>
    public (bool IsSatisfied, string? ErrorMessage) IsSatisfiedBy(Entities.Character character, Item item, EquipmentSlotType slot, LangType langType = LangType.PT_BR)
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
            return (false, GameErrors.GetEquipmentSlotRequirementError(character.Account.PreferredLanguage));
        
        // 4. Personagem atende aos requisitos do item
        if (character.Progress.Level < item.RequiredLevel)
            return (false, GameErrors.GetLevelRequirementError(item.RequiredLevel, langType));
        
        if (item.RequiredVocation != VocationType.None && item.RequiredVocation != character.Vocation)
            return (false, GameErrors.GetVocationRequirementError(item.RequiredVocation, langType));
        
        if (character.BaseStats.Strength < item.RequiredStats.Strength)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Strength, langType));
        
        if (character.BaseStats.Dexterity < item.RequiredStats.Dexterity)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Dexterity, langType));
        
        if (character.BaseStats.Intelligence < item.RequiredStats.Intelligence)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Intelligence, langType));
        
        if (character.BaseStats.Constitution < item.RequiredStats.Constitution)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Constitution, langType));
        
        if (character.BaseStats.Spirit < item.RequiredStats.Spirit)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Spirit, langType));
        
        return (true, null);
    }
}
