// Domain/Entities/Item.cs
using Game.Domain.Enums;

namespace Game.Domain.Entities;

/// <summary>
/// Template de item (dados estáticos)
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public class Item : BaseEntity
{
    public string Name { get; init; } = null!;
    public string Description { get; init; } = string.Empty; // ADICIONADO
    public ItemType Type { get; init; }
    public int StackSize { get; init; } = 1;
    public int Weight { get; init; }
    public string IconPath { get; init; } = string.Empty; // ADICIONADO
    
    // Stats de Equipamento (aplicam-se apenas se Type for Weapon ou Armor)
    public virtual ItemStats? Stats { get; init; }
    
    // Requisitos para equipar (ADICIONADO)
    public int RequiredLevel { get; init; }
    public VocationType? RequiredVocation { get; init; }
}

/// <summary>
/// Stats de um item equipável
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
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
}