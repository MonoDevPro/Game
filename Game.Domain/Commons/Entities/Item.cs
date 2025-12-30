using System.ComponentModel.DataAnnotations.Schema;
using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Attributes;

namespace Game.Domain.Commons.Entities;

/// <summary>
/// Template de item (dados estáticos de definição).
/// </summary>
public class Item : BaseEntity
{
    public string Name { get; init; } = null!;
    public string Description { get; init; } = string.Empty;
    public ItemType Type { get; init; }
    public EquipmentSlotType Slot { get; init; }
    public ItemRarity Rarity { get; init; } = ItemRarity.Common;
    public int StackSize { get; init; } = 1;
    public int Weight { get; init; }
    public string IconPath { get; init; } = string.Empty;
    
    // Requerimentos de uso
    public int RequiredLevel { get; init; }
    public VocationType RequiredVocation { get; init; }
    [NotMapped]
    public BaseStats RequiredStats { get; init; }
    
    // Bônus concedidos ao equipar o item
    [NotMapped]
    public virtual BaseStats BonusStats { get; init; }
    public ItemStats? Stats { get; set; }
    
    /// <summary>
    /// Verifica se o item é empilhável.
    /// </summary>
    public bool IsStackable => StackSize > 1;
}