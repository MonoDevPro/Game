using Game.Domain.ValueObjects.Equipment;
using Game.Domain.ValueObjects.Character;
using Game.Domain.ValueObjects.Attributes;
using Game.Domain.Commons;
using Game.Domain.Enums;
using Game.Domain.Extensions;
using Game.Domain.Player;

namespace Game.Domain.Entities;

/// <summary>
/// Personagem jogável.
/// Entidade raiz do agregado de personagem.
/// </summary>
public class Character : BaseEntity
{
    public string Name { get; init; } = null!;
    public GenderType Gender { get; set; }
    public VocationType Vocation { get; set; }
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public DirectionType Direction { get; set; }
    public BaseStats BaseStats { get; set; }
    public Progress Progress { get; set; } = Progress.Initial;
    
    // Vitais atuais (HP/MP salvos)
    public int CurrentHp { get; set; }
    public int CurrentMp { get; set; }
    
    // Relacionamentos
    public int AccountId { get; init; }
    public Account Account { get; set; } = null!;
    
    // Um personagem tem um inventário (1:1)
    public Inventory Inventory { get; set; } = null!;
    
    // Um personagem tem equipamentos (1:1)
    public Equipments Equipments { get; set; }
}
