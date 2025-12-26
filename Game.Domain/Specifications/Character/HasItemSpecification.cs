using Game.Domain.Entities;

namespace Game.Domain.Specifications.Character;

/// <summary>
/// Especificação para verificar se um personagem possui um item no inventário.
/// </summary>
public class HasItemSpecification
{
    /// <summary>
    /// Verifica se o personagem possui a quantidade especificada do item.
    /// </summary>
    /// <param name="character">O personagem a ser verificado.</param>
    /// <param name="itemId">O ID do item a ser verificado.</param>
    /// <param name="requiredQuantity">A quantidade mínima necessária do item.</param>
    /// <returns>True se o personagem possui a quantidade especificada, false caso contrário.</returns>
    public bool IsSatisfiedBy(Entities.Character character, int itemId, int requiredQuantity = 1)
    {
        if (character.Inventory == null) return false;
        if (itemId <= 0) return false;
        if (requiredQuantity <= 0) return false;
        
        int availableQuantity = character.Inventory.GetItemCount(itemId);
        return availableQuantity >= requiredQuantity;
    }

    /// <summary>
    /// Retorna quantos itens do tipo especificado o personagem possui.
    /// </summary>
    /// <param name="character">O personagem a ser verificado.</param>
    /// <param name="itemId">O ID do item a ser contado.</param>
    /// <returns>A quantidade total do item no inventário do personagem.</returns>
    public int GetItemCount(Entities.Character character, int itemId)
    {
        if (character.Inventory == null) return 0;
        if (itemId <= 0) return 0;
        
        return character.Inventory.GetItemCount(itemId);
    }

    /// <summary>
    /// Retorna a razão pela qual a verificação falhou.
    /// </summary>
    /// <param name="character">O personagem a ser verificado.</param>
    /// <param name="itemId">O ID do item a ser verificado.</param>
    /// <param name="requiredQuantity">A quantidade mínima necessária do item.</param>
    /// <returns>Mensagem de erro se houver alguma restrição, ou string vazia se não houver problemas.</returns>
    public string GetFailureReason(Entities.Character character, int itemId, int requiredQuantity = 1)
    {
        if (character.Inventory == null)
            return "Character has no inventory";
        
        if (itemId <= 0)
            return "Invalid item ID";
        
        if (requiredQuantity <= 0)
            return "Required quantity must be positive";
        
        int availableQuantity = character.Inventory.GetItemCount(itemId);
        if (availableQuantity < requiredQuantity)
            return $"Insufficient items. Required: {requiredQuantity}, Available: {availableQuantity}";
        
        return string.Empty;
    }
}
