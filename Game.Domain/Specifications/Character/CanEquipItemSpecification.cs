using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.Extensions;
using Game.Domain.ValueObjects.Attributes;

namespace Game.Domain.Specifications.Character;

/// <summary>
/// Especificação para validar se um personagem pode equipar um item.
/// </summary>
public class CanEquipItemSpecification
{
    /// <summary>
    /// Verifica se o personagem pode equipar o item no slot especificado.
    /// </summary>
    /// <param name="character">O personagem que tentará equipar o item.</param>
    /// <param name="item">O item a ser equipado.</param>
    /// <param name="slot">O slot do equipamento onde o item será equipado.</param>
    /// <returns>Uma tupla contendo um booleano indicando se a especificação foi satisfeita e uma mensagem de erro se não foi.</returns>
    public (bool IsSatisfied, string? ErrorMessage) IsSatisfiedBy(Entities.Character character, Item item, EquipmentSlotType slot)
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
            return (false, "Item does not match equipment slot");
        
        // 4. Personagem atende aos requisitos do item
        int playerLevel = character.Progress.Level;
        int requiredLevel = item.RequiredLevel;
        VocationType playerVocation = character.Vocation;
        VocationType requiredVocation = item.RequiredVocation;
        BaseStats requiredStats = item.RequiredStats;
        LangType langType = character.Account.PreferredLanguage;
        BaseStats playerStats = character.BaseStats;
        
        if (playerLevel < requiredLevel)
            return (false, GameErrors.GetLevelRequirementError(requiredLevel, langType));
        
        if (requiredVocation != VocationType.None && requiredVocation != playerVocation)
            return (false, GameErrors.GetVocationRequirementError(requiredVocation, langType));
        
        if (playerStats.Strength < requiredStats.Strength)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Strength, langType));
        
        if (playerStats.Dexterity < requiredStats.Dexterity)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Dexterity, langType));
        
        if (playerStats.Intelligence < requiredStats.Intelligence)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Intelligence, langType));
        
        if (playerStats.Constitution < requiredStats.Constitution)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Constitution, langType));
        
        if (playerStats.Spirit < requiredStats.Spirit)
            return (false, GameErrors.GetStatRequirementError(AttributeType.Spirit, langType));
        
        return (true, null);
    }
    
    /// <summary>
    /// Retorna a razão pela qual o equip falhou, se aplicável.
    /// </summary>
    /// <param name="character">O personagem que tentará equipar o item.</param>
    /// <param name="itemId">O ID do item a ser equipado.</param>
    /// <param name="slot">O slot do equipamento onde o item será equipado.</param>
    /// <returns>Mensagem de erro se houver alguma restrição, ou string vazia se não houver problemas.</returns>
    public string GetFailureReason(Entities.Character character, int itemId, EquipmentSlotType slot)
    {
        if (!character.IsActive)
            return "Character must be active to equip items";
        
        if (itemId <= 0)
            return "Invalid item ID";
        
        if (!character.Inventory.HasItem(itemId, 1))
            return "Character does not possess the item";
        
        return string.Empty;
    }
}
