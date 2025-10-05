// Domain/Entities/Character.cs
using Game.Domain.Enums;

namespace Game.Domain.Entities;

/// <summary>
/// Personagem jogável
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public class Character : BaseEntity
{
    public string Name { get; init; } = null!;
    public Gender Gender { get; set; } = Gender.Unknown;
    public VocationType Vocation { get; set; } = VocationType.Warrior;
    
    // Posição no mundo
    public Direction Direction { get; set; } = Direction.South;
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    
    // Relacionamentos
    public int AccountId { get; init; }
    public virtual Account Account { get; init; } = null!;
    
    // Um personagem tem um inventário (1:1)
    public virtual Inventory Inventory { get; init; } = null!;
    
    // Um personagem tem stats (1:1)
    public virtual Stats Stats { get; init; } = null!;
    
    // Um personagem tem múltiplos slots de equipamento (1:N)
    public virtual ICollection<EquipmentSlot> Equipment { get; init; } = new List<EquipmentSlot>();
}