using Game.Domain.Commons.Enums;
using Game.Domain.Player;

namespace Game.Domain.Commons.Entities;

/// <summary>
/// Personagem jogável.
/// Entidade raiz do agregado de personagem.
/// </summary>
public class Character : BaseEntity, IAggregateRoot
{
    public string Name { get; init; } = null!;
    public GenderType Gender { get; set; }
    public VocationType Vocation { get; set; }
    
    public int Direction { get; set; }
    public int Map { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public int PositionZ { get; set; }
    public int X { get => PositionX; set => PositionX = value; }
    public int Y { get => PositionY; set => PositionY = value; }
    
    public int Level { get; set; }
    public int Experience { get; set; }
    
    public int Strength { get; set; }
    public int Dexterity { get; set; }
    public int Intelligence { get; set; }
    public int Constitution { get; set; }
    public int Spirit { get; set; }
    
    // Um personagem tem equipamentos (1:1)
    public int EquipmentsId { get; init; }
    public Equipments Equipments { get; set; } = null!;
    
    // Relacionamentos
    public int AccountId { get; init; }
    public Account Account { get; set; } = null!;
    
    // Um personagem tem um inventário (1:1)
    public int InventoryId { get; init; }
    public Inventory Inventory { get; set; } = null!;

    // Stats persistidos (1:1)
    public PlayerSimulationAttributes Stats { get; set; } = null!;

    // Slots de equipamento persistidos
    public ICollection<EquipmentSlot> Equipment { get; set; } = new List<EquipmentSlot>();
}
