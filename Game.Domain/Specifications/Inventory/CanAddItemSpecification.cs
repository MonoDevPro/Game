using Game.Domain.Entities;

namespace Game.Domain.Specifications.Inventory;

/// <summary>
/// Especificação para validar se um item pode ser adicionado ao inventário.
/// </summary>
public class CanAddItemSpecification
{
    /// <summary>
    /// Verifica se o inventário tem espaço para adicionar a quantidade especificada do item.
    /// </summary>
    /// <param name="inventory">O inventário onde o item será adicionado.</param>
    /// <param name="item">O item a ser adicionado.</param>
    /// <param name="quantity">A quantidade do item a ser adicionada.</param>
    /// <returns>True se o item pode ser adicionado, false caso contrário.</returns>
    public bool IsSatisfiedBy(Entities.Inventory inventory, Item item, int quantity)
    {
        if (inventory == null) return false;
        if (item == null) return false;
        if (quantity <= 0) return false;
        
        // Se inventário está cheio e item não é empilhável
        if (inventory.IsFull && !item.IsStackable)
            return false;
        
        // Se item é empilhável, verificar se pode empilhar em slots existentes
        if (item.IsStackable)
        {
            int currentCount = inventory.GetItemCount(item.Id);
            int totalAfterAdd = currentCount + quantity;
            
            // Calcular quantos slots serão necessários
            int slotsNeeded = (totalAfterAdd + item.StackSize - 1) / item.StackSize;
            int currentSlots = (currentCount + item.StackSize - 1) / item.StackSize;
            int newSlotsNeeded = slotsNeeded - currentSlots;
            
            return inventory.FreeSlots >= newSlotsNeeded;
        }
        
        // Item não empilhável: precisa de 'quantity' slots livres
        return inventory.FreeSlots >= quantity;
    }

    /// <summary>
    /// Retorna a razão pela qual o item não pode ser adicionado.
    /// </summary>
    /// <param name="inventory">O inventário onde o item seria adicionado.</param>
    /// <param name="item">O item a ser adicionado.</param>
    /// <param name="quantity">A quantidade do item a ser adicionada.</param>
    /// <returns>Mensagem de erro se houver alguma restrição, ou string vazia se não houver problemas.</returns>
    public string GetFailureReason(Entities.Inventory inventory, Item item, int quantity)
    {
        if (inventory == null)
            return "Inventory cannot be null";
        
        if (item == null)
            return "Item cannot be null";
        
        if (quantity <= 0)
            return "Quantity must be positive";
        
        if (inventory.IsFull && !item.IsStackable)
            return "Inventory is full";
        
        if (item.IsStackable)
        {
            int currentCount = inventory.GetItemCount(item.Id);
            int totalAfterAdd = currentCount + quantity;
            int slotsNeeded = (totalAfterAdd + item.StackSize - 1) / item.StackSize;
            int currentSlots = (currentCount + item.StackSize - 1) / item.StackSize;
            int newSlotsNeeded = slotsNeeded - currentSlots;
            
            if (inventory.FreeSlots < newSlotsNeeded)
                return $"Not enough space. Need {newSlotsNeeded} free slots, have {inventory.FreeSlots}";
        }
        else
        {
            if (inventory.FreeSlots < quantity)
                return $"Not enough space. Need {quantity} free slots, have {inventory.FreeSlots}";
        }
        
        return string.Empty;
    }
}
