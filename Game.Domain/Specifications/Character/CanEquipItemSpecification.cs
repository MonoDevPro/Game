using Game.Domain.Entities;
using Game.Domain.Enums;

namespace Game.Domain.Specifications.Character;

/// <summary>
/// Especificação para validar se um personagem pode equipar um item.
/// </summary>
public class CanEquipItemSpecification
{
    /// <summary>
    /// Verifica se o personagem pode equipar o item no slot especificado.
    /// </summary>
    public bool IsSatisfiedBy(Entities.Character character, int itemId, EquipmentSlotType slot)
    {
        // TODO: Implementar validação completa quando Item entity estiver disponível
        // Verificações necessárias:
        // 1. Item existe
        // 2. Item é equipável
        // 3. Item corresponde ao slot
        // 4. Personagem atende requisitos de nível
        // 5. Personagem atende requisitos de vocação
        // 6. Personagem possui o item no inventário
        
        if (!character.IsActive)
            return false;
            
        if (itemId <= 0)
            return false;
        
        return true;
    }

    /// <summary>
    /// Retorna a razão pela qual o equip falhou, se aplicável.
    /// </summary>
    public string GetFailureReason(Entities.Character character, int itemId, EquipmentSlotType slot)
    {
        if (!character.IsActive)
            return "Character must be active to equip items";
        
        if (itemId <= 0)
            return "Invalid item ID";
        
        // TODO: Adicionar mais validações quando Item entity estiver disponível
        
        return string.Empty;
    }
}
