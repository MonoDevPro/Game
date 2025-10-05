namespace Game.Domain.Entities;

/// <summary>
/// Inventário do personagem
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public class Inventory : BaseEntity
{
    public int Capacity { get; init; } = 30; // Capacidade padrão
    
    // Relacionamentos
    public int CharacterId { get; init; }
    public virtual Character Character { get; init; } = null!;
    
    public virtual ICollection<InventorySlot> Slots { get; init; } = new List<InventorySlot>();
}

/// <summary>
/// Slot do inventário
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public class InventorySlot : BaseEntity
{
    public int SlotIndex { get; set; }
    public int Quantity { get; set; } = 1;
    
    // Relacionamentos
    public int InventoryId { get; init; }
    public virtual Inventory Inventory { get; init; } = null!;
    
    public int? ItemId { get; init; }
    public virtual Item? Item { get; init; }
}