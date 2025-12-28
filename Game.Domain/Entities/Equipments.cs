using Game.Domain.Commons;

namespace Game.Domain.Entities;

public class Equipments : BaseEntity
{
    public int? HeadItemId { get; set; }
    public int? ChestItemId { get; set; }
    public int? LegsItemId { get; set; }
    public int? FeetItemId { get; set; }
    public int? HandsItemId { get; set; }
    public int? MainHandItemId { get; set; }
    public int? OffHandItemId { get; set; }
    public int? Accessory1ItemId { get; set; }
    public int? Accessory2ItemId { get; set; }
    public int? Accessory3ItemId { get; set; }
    
    public int CharacterId { get; init; }
    public Character Character { get; set; } = null!;
    
    public Span<int?> GetAllEquippedItemIds()
    {
        return new[]
        {
            HeadItemId,
            ChestItemId,
            LegsItemId,
            FeetItemId,
            HandsItemId,
            MainHandItemId,
            OffHandItemId,
            Accessory1ItemId,
            Accessory2ItemId,
            Accessory3ItemId
        };
    }
}
