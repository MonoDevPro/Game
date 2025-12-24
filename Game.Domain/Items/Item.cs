using Game.Domain.Attributes.Stats.ValueObjects;
using Game.Domain.Commons;
using Game.Domain.Commons.Enums;
using Game.Domain.Player;

namespace Game.Domain.Items;

/// <summary>
/// Template de item (dados estáticos de definição).
/// </summary>
public class Item : BaseEntity
{
    public string Name { get; init; } = null!;
    public string Description { get; init; } = string.Empty;
    public ItemType Type { get; init; }
    public int StackSize { get; init; } = 1;
    public int Weight { get; init; }
    public ItemRarity Rarity { get; init; } = ItemRarity.Common;
    public string IconPath { get; init; } = string.Empty;
    
    // Stats de Equipamento (aplicam-se apenas se Type for Weapon ou Armor)
    public virtual ItemStats? Stats { get; init; }
    
    // Requisitos para equipar
    public int RequiredLevel { get; init; }
    public VocationType? RequiredVocation { get; init; }
    
    /// <summary>
    /// Verifica se o item é empilhável.
    /// </summary>
    public bool IsStackable => StackSize > 1;
    
    /// <summary>
    /// Verifica se o item é equipável.
    /// </summary>
    public bool IsEquipable => Type is ItemType.Weapon or ItemType.Armor or ItemType.Accessory;
    
    /// <summary>
    /// Verifica se um personagem pode equipar este item.
    /// </summary>
    public bool CanBeEquippedBy(int playerLevel, VocationType playerVocation)
    {
        if (playerLevel < RequiredLevel) return false;
        if (RequiredVocation.HasValue && RequiredVocation.Value != playerVocation) return false;
        return true;
    }
}

/// <summary>
/// Raridade do item.
/// </summary>
public enum ItemRarity : byte
{
    Common = 0,
    Uncommon = 1,
    Rare = 2,
    Epic = 3,
    Legendary = 4,
    Mythic = 5
}

/// <summary>
/// Stats de um item equipável.
/// Entidade persistível com lógica de domínio.
/// </summary>
public class ItemStats : BaseEntity
{
    // Bônus de Atributos
    public int BonusStrength { get; init; }
    public int BonusDexterity { get; init; }
    public int BonusIntelligence { get; init; }
    public int BonusConstitution { get; init; }
    public int BonusSpirit { get; init; }
    
    // Bônus de Combate Diretos
    public int BonusPhysicalAttack { get; init; }
    public int BonusMagicAttack { get; init; }
    public int BonusPhysicalDefense { get; init; }
    public int BonusMagicDefense { get; init; }
    
    // Bônus de Velocidade
    public float BonusAttackSpeed { get; init; }
    public float BonusMovementSpeed { get; init; }
    
    // Relacionamento
    public int ItemId { get; init; }
    public virtual Item Item { get; init; } = null!;
    
    /// <summary>
    /// Converte para Stats do domínio (apenas atributos base).
    /// </summary>
    public BaseStats ToStats()
    {
        return new BaseStats(
            BonusStrength,
            BonusDexterity,
            BonusIntelligence,
            BonusConstitution,
            BonusSpirit);
    }
    
    /// <summary>
    /// Cria uma versão leve para uso no ECS (sem referências).
    /// </summary>
    public ItemStatsSnapshot ToSnapshot()
    {
        return new ItemStatsSnapshot(
            BonusStrength, BonusDexterity, BonusIntelligence, BonusConstitution, BonusSpirit,
            BonusPhysicalAttack, BonusMagicAttack, BonusPhysicalDefense, BonusMagicDefense,
            BonusAttackSpeed, BonusMovementSpeed);
    }
}

/// <summary>
/// Snapshot imutável de ItemStats para uso no ECS.
/// Value object sem referências a entidades.
/// </summary>
public readonly record struct ItemStatsSnapshot(
    int BonusStrength,
    int BonusDexterity,
    int BonusIntelligence,
    int BonusConstitution,
    int BonusSpirit,
    int BonusPhysicalAttack,
    int BonusMagicAttack,
    int BonusPhysicalDefense,
    int BonusMagicDefense,
    float BonusAttackSpeed,
    float BonusMovementSpeed)
{
    public static ItemStatsSnapshot Zero => default;
    
    public BaseStats ToStats() => new(
        BonusStrength, BonusDexterity, BonusIntelligence, BonusConstitution, BonusSpirit);
    
    public static ItemStatsSnapshot operator +(ItemStatsSnapshot a, ItemStatsSnapshot b) => new(
        a.BonusStrength + b.BonusStrength,
        a.BonusDexterity + b.BonusDexterity,
        a.BonusIntelligence + b.BonusIntelligence,
        a.BonusConstitution + b.BonusConstitution,
        a.BonusSpirit + b.BonusSpirit,
        a.BonusPhysicalAttack + b.BonusPhysicalAttack,
        a.BonusMagicAttack + b.BonusMagicAttack,
        a.BonusPhysicalDefense + b.BonusPhysicalDefense,
        a.BonusMagicDefense + b.BonusMagicDefense,
        a.BonusAttackSpeed + b.BonusAttackSpeed,
        a.BonusMovementSpeed + b.BonusMovementSpeed);
}