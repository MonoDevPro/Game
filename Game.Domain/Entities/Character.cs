// Domain/Entities/Character.cs
using Game.Domain.Enums;

namespace Game.Domain.Entities;

/// <summary>
/// Personagem jogável
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public sealed class Character : BaseEntity
{
    public string Name { get; init; } = null!;
    public Gender Gender { get; set; } = Gender.Unknown;
    public VocationType Vocation { get; set; } = VocationType.Unknown;
    
    // Posição no mundo
    public byte Direction { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int PositionZ { get; set; }
    
    // Relacionamentos
    public int AccountId { get; init; }
    public Account Account { get; set; } = null!;
    
    // Um personagem tem um inventário (1:1)
    public Inventory Inventory { get; set; } = null!;
    
    // Um personagem tem stats (1:1)
    public Stats Stats { get; set; } = null!;
    
    // Um personagem tem múltiplos slots de equipamento (1:N)
    public ICollection<EquipmentSlot> Equipment { get; init; } = new List<EquipmentSlot>();
    
    public override string ToString() => $"Character(Id={Id}, Name={Name}, Vocation={Vocation}, Level={Stats.Level}, Pos=({PositionX},{PositionY},{PositionZ}))";
    
}