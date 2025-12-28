using Game.Domain.Commons;

namespace Game.Domain.Entities;

/// <summary>
/// Personagem jogável.
/// Entidade raiz do agregado de personagem.
/// </summary>
public class Character : BaseEntity, IAggregateRoot
{
    public string Name { get; init; } = null!;
    public int Gender { get; set; }
    public int Vocation { get; set; }
    
    public int Direction { get; set; }
    public int Map { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    
    public int Level { get; set; }
    public int Experience { get; set; }
    
    public double Strength { get; set; }
    public double Dexterity { get; set; }
    public double Intelligence { get; set; }
    public double Constitution { get; set; }
    public double Spirit { get; set; }
    
    public int Hp { get; set; }
    public int MaxHp { get; set; }
    public int Mp { get; set; }
    public int MaxMp { get; set; }
    
    // Um personagem tem equipamentos (1:1)
    public int EquipmentsId { get; init; }
    public Equipments Equipments { get; set; } = null!;
    
    // Relacionamentos
    public int AccountId { get; init; }
    public Account Account { get; set; } = null!;
    
    // Um personagem tem um inventário (1:1)
    public int InventoryId { get; init; }
    public Inventory Inventory { get; set; } = null!;
}