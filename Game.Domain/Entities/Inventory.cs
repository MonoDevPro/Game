using Game.Domain.Commons;

namespace Game.Domain.Entities;

/// <summary>
/// Inventário do personagem.
/// Gerencia slots, capacidade e operações de itens.
/// </summary>
public class Inventory
{
    public int CharacterId { get; init; }
    public int Capacity { get; private set; }
    
    private readonly List<InventorySlot> _slots = new();
    public IReadOnlyList<InventorySlot> Slots => _slots;
    
    /// <summary>
    /// Número de slots ocupados.
    /// </summary>
    public int UsedSlots => _slots.Count;
    
    /// <summary>
    /// Slots livres disponíveis.
    /// </summary>
    public int FreeSlots => Capacity - UsedSlots;
    
    /// <summary>
    /// Verifica se o inventário está cheio.
    /// </summary>
    public bool IsFull => UsedSlots >= Capacity;
    
    /// <summary>
    /// Tenta adicionar um item ao inventário.
    /// Se o item for empilhável, tenta empilhar em slots existentes primeiro.
    /// </summary>
    public InventoryResult TryAddItem(int itemId, int quantity, int maxStackSize = 1)
    {
        if (quantity <= 0)
            return InventoryResult.Fail("Quantidade inválida");
        
        int remaining = quantity;
        
        // Se empilhável, tenta empilhar em slots existentes
        if (maxStackSize > 1)
        {
            for (int i = 0; i < _slots.Count && remaining > 0; i++)
            {
                var slot = _slots[i];
                if (slot.ItemId == itemId && slot.Quantity < maxStackSize)
                {
                    int canAdd = Math.Min(remaining, maxStackSize - slot.Quantity);
                    slot.Quantity += canAdd;
                    remaining -= canAdd;
                }
            }
        }
        
        // Adiciona em novos slots
        while (remaining > 0)
        {
            if (IsFull)
                return remaining == quantity 
                    ? InventoryResult.Fail("Inventário cheio")
                    : InventoryResult.Partial(quantity - remaining, "Inventário cheio, adicionado parcialmente");
            
            int toAdd = Math.Min(remaining, maxStackSize);
            int newIndex = FindNextFreeSlotIndex();
            _slots.Add(InventorySlot.Create(newIndex, itemId, toAdd));
            remaining -= toAdd;
        }
        
        return InventoryResult.Success(quantity);
    }
    
    /// <summary>
    /// Remove uma quantidade de um item do inventário.
    /// </summary>
    public InventoryResult TryRemoveItem(int itemId, int quantity)
    {
        if (quantity <= 0)
            return InventoryResult.Fail("Quantidade inválida");
        
        int totalAvailable = GetItemCount(itemId);
        if (totalAvailable < quantity)
            return InventoryResult.Fail($"Quantidade insuficiente. Disponível: {totalAvailable}");
        
        int remaining = quantity;
        
        // Remove dos slots (do último para o primeiro para preservar índices)
        for (int i = _slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = _slots[i];
            if (slot.ItemId != itemId) continue;
            
            if (slot.Quantity <= remaining)
            {
                remaining -= slot.Quantity;
                _slots.RemoveAt(i);
            }
            else
            {
                slot.Quantity -= remaining;
                remaining = 0;
            }
        }
        
        return InventoryResult.Success(quantity);
    }
    
    /// <summary>
    /// Conta a quantidade total de um item no inventário.
    /// </summary>
    public int GetItemCount(int itemId)
    {
        return _slots.Where(s => s.ItemId == itemId).Sum(s => s.Quantity);
    }
    
    /// <summary>
    /// Verifica se contém pelo menos a quantidade especificada de um item.
    /// </summary>
    public bool HasItem(int itemId, int quantity = 1)
    {
        return GetItemCount(itemId) >= quantity;
    }
    
    /// <summary>
    /// Obtém o slot em um índice específico.
    /// </summary>
    public InventorySlot? GetSlot(int slotIndex)
    {
        return _slots.FirstOrDefault(s => s.SlotIndex == slotIndex);
    }
    
    /// <summary>
    /// Move item de um slot para outro.
    /// </summary>
    public bool TryMoveItem(int fromSlotIndex, int toSlotIndex)
    {
        if (toSlotIndex < 0 || toSlotIndex >= Capacity)
            return false;
        
        var slot = _slots.FirstOrDefault(s => s.SlotIndex == fromSlotIndex);
        if (slot == null) return false;
        
        slot.SlotIndex = toSlotIndex;
        return true;
    }
    
    /// <summary>
    /// Expande a capacidade do inventário.
    /// </summary>
    public bool TryExpandCapacity(int additionalSlots)
    {
        if (additionalSlots <= 0) return false;
        if (Capacity + additionalSlots > GameConstants.Inventory.MaxCapacity)
            return false;
        
        Capacity += additionalSlots;
        return true;
    }
    
    /// <summary>
    /// Limpa todo o inventário.
    /// </summary>
    public void Clear()
    {
        _slots.Clear();
    }
    
    private int FindNextFreeSlotIndex()
    {
        var usedIndices = _slots.Select(s => s.SlotIndex).ToHashSet();
        for (int i = 0; i < Capacity; i++)
        {
            if (!usedIndices.Contains(i))
                return i;
        }
        return _slots.Count;
    }
}

/// <summary>
/// Slot do inventário - Entidade persistível.
/// </summary>
public class InventorySlot
{
    public int Id { get; set; }
    public int InventoryId { get; set; }
    public int SlotIndex { get; set; }
    public int ItemId { get; set; }
    public int Quantity { get; set; }
    
    public bool IsEmpty => ItemId <= 0 || Quantity <= 0;
    
    /// <summary>
    /// Cria um slot de inventário.
    /// </summary>
    public static InventorySlot Create(int slotIndex, int itemId, int quantity) =>
        new() { SlotIndex = slotIndex, ItemId = itemId, Quantity = quantity };
}

/// <summary>
/// Resultado de uma operação de inventário.
/// </summary>
public readonly record struct InventoryResult(
    bool IsSuccess,
    int AffectedQuantity,
    string? Message = null)
{
    public bool IsPartial => IsSuccess && Message != null;
    
    public static InventoryResult Success(int quantity) => new(true, quantity);
    public static InventoryResult Partial(int quantity, string message) => new(true, quantity, message);
    public static InventoryResult Fail(string message) => new(false, 0, message);
}