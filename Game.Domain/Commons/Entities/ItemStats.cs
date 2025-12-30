namespace Game.Domain.Commons.Entities;

/// <summary>
/// Entidade de atributos bônus de um item equipável.
/// </summary>
public class ItemStats : BaseEntity
{
    public int BonusStrength { get; set; }
    public int BonusDexterity { get; set; }
    public int BonusIntelligence { get; set; }
    public int BonusConstitution { get; set; }
    public int BonusSpirit { get; set; }
    public int BonusPhysicalAttack { get; set; }
    public int BonusMagicAttack { get; set; }
    public int BonusPhysicalDefense { get; set; }
    public int BonusMagicDefense { get; set; }
    public float BonusAttackSpeed { get; set; }
    public float BonusMovementSpeed { get; set; }

    public int ItemId { get; set; }
    public Item Item { get; set; } = null!;
}
