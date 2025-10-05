using Game.Domain.Enums;

namespace Game.Domain.Entities;

/// <summary>
/// Slot de equipamento do personagem
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public class EquipmentSlot : BaseEntity
{
    public EquipmentSlotType SlotType { get; init; }
    
    // Um slot pode ter um item equipado (opcional)
    public int? ItemId { get; set; } // Nullable - slot pode estar vazio
    public virtual Item? Item { get; set; } // Nullable porque ItemId é nullable
    
    // Relacionamentos
    // ✅ APENAS relacionamento com Character (removido Equipments)
    public int CharacterId { get; init; }
    public virtual Character Character { get; init; } = null!;
}
