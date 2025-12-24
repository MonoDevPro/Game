using Game.Domain.Attributes.Equipments.ValueObjects;
using Game.Domain.Attributes.Progress.ValueObjects;
using Game.Domain.Attributes.Stats.ValueObjects;
using Game.Domain.Commons;
using Game.Domain.Enums;
using Game.Domain.Commons.Extensions;

namespace Game.Domain.Player;

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
    
    // Progressão
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
    
    /// <summary>
    /// Calcula os atributos completos do personagem baseado na vocação e progressão.
    /// </summary>
    public PlayerAttributes CalculateAttributes(BaseStats equipmentBonus = default)
    {
        var vocationInfo = Vocation.GetInfo();
        return PlayerAttributes.Create(
            Progress,
            vocationInfo.BaseStats,
            equipmentBonus,
            vocationInfo.GrowthModifiers,
            CurrentHp > 0 ? CurrentHp : null,
            CurrentMp > 0 ? CurrentMp : null);
    }
    
    /// <summary>
    /// Verifica se pode promover para a vocação especificada.
    /// </summary>
    public bool CanPromoteTo(VocationType targetVocation)
    {
        return Vocation.CanPromote(targetVocation, Progress.Level);
    }
    
    /// <summary>
    /// Promove o personagem para uma nova vocação.
    /// </summary>
    public bool TryPromoteTo(VocationType targetVocation)
    {
        if (!CanPromoteTo(targetVocation)) return false;
        Vocation = targetVocation;
        return true;
    }
}
