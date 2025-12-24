using Game.Domain.Commons;
using Game.Domain.Items;

namespace Game.Domain.Player;

/// <summary>
/// Gerencia os equipamentos do personagem.
/// Contém lógica de equipar, desequipar e calcular bônus.
/// </summary>
public class Equipments
{
    public int CharacterId { get; init; }
    
    private readonly Dictionary<EquipmentSlotType, EquipmentSlot> _slots = new();
    
    public IReadOnlyDictionary<EquipmentSlotType, EquipmentSlot> Slots => _slots;
    
    /// <summary>
    /// Inicializa com slots vazios.
    /// </summary>
    public Equipments()
    {
        foreach (EquipmentSlotType slotType in Enum.GetValues<EquipmentSlotType>())
        {
            _slots[slotType] = new EquipmentSlot(slotType);
        }
    }
    
    /// <summary>
    /// Obtém o item equipado em um slot.
    /// </summary>
    public EquipmentSlot GetSlot(EquipmentSlotType slotType)
    {
        return _slots.TryGetValue(slotType, out var slot) ? slot : new EquipmentSlot(slotType);
    }
    
    /// <summary>
    /// Verifica se um slot está ocupado.
    /// </summary>
    public bool IsSlotOccupied(EquipmentSlotType slotType)
    {
        return _slots.TryGetValue(slotType, out var slot) && !slot.IsEmpty;
    }
    
    /// <summary>
    /// Tenta equipar um item em um slot.
    /// </summary>
    public EquipmentResult TryEquip(EquipmentSlotType slotType, int itemId, Item item, int playerLevel, VocationType playerVocation)
    {
        // Valida requisitos do item
        if (item.RequiredLevel > playerLevel)
            return EquipmentResult.Fail($"Nível {item.RequiredLevel} necessário");
        
        if (item.RequiredVocation.HasValue && item.RequiredVocation.Value != playerVocation)
            return EquipmentResult.Fail($"Vocação {item.RequiredVocation} necessária");
        
        // Valida tipo de item para o slot
        if (!IsValidItemForSlot(slotType, item.Type))
            return EquipmentResult.Fail($"Item não pode ser equipado neste slot");
        
        // Guarda item anterior (se houver)
        var currentSlot = GetSlot(slotType);
        var previousItemId = currentSlot.ItemId;
        
        // Equipa o novo item
        currentSlot.ItemId = itemId;
        _slots[slotType] = currentSlot;
        
        return EquipmentResult.Success(previousItemId > 0 ? previousItemId : null);
    }
    
    /// <summary>
    /// Remove o item de um slot.
    /// </summary>
    public EquipmentResult TryUnequip(EquipmentSlotType slotType)
    {
        if (!IsSlotOccupied(slotType))
            return EquipmentResult.Fail("Slot já está vazio");
        
        var currentSlot = GetSlot(slotType);
        var previousItemId = currentSlot.ItemId;
        currentSlot.ItemId = 0;
        
        return EquipmentResult.Success(previousItemId);
    }
    
    /// <summary>
    /// Calcula os bônus totais de stats dos equipamentos.
    /// </summary>
    public Stats CalculateTotalStatsBonus(Func<int, ItemStats?> getItemStats)
    {
        int strength = 0, dexterity = 0, intelligence = 0, constitution = 0, spirit = 0;
        
        foreach (var slot in _slots.Values)
        {
            if (slot.IsEmpty) continue;
            
            if (getItemStats(slot.ItemId) is not { } stats) continue;
            
            strength += stats.BonusStrength;
            dexterity += stats.BonusDexterity;
            intelligence += stats.BonusIntelligence;
            constitution += stats.BonusConstitution;
            spirit += stats.BonusSpirit;
        }
        
        return new Stats(strength, dexterity, intelligence, constitution, spirit);
    }
    
    /// <summary>
    /// Calcula os bônus de combate diretos dos equipamentos.
    /// </summary>
    public EquipmentCombatBonus CalculateCombatBonus(Func<int, ItemStats?> getItemStats)
    {
        int physAtk = 0, magAtk = 0, physDef = 0, magDef = 0;
        float atkSpd = 0, movSpd = 0;
        
        foreach (var slot in _slots.Values)
        {
            if (slot.IsEmpty) continue;
            
            if (getItemStats(slot.ItemId) is not { } stats) continue;
            
            physAtk += stats.BonusPhysicalAttack;
            magAtk += stats.BonusMagicAttack;
            physDef += stats.BonusPhysicalDefense;
            magDef += stats.BonusMagicDefense;
            atkSpd += stats.BonusAttackSpeed;
            movSpd += stats.BonusMovementSpeed;
        }
        
        return new EquipmentCombatBonus(physAtk, magAtk, physDef, magDef, atkSpd, movSpd);
    }
    
    /// <summary>
    /// Obtém todos os IDs de itens equipados.
    /// </summary>
    public IEnumerable<int> GetEquippedItemIds()
    {
        return _slots.Values.Where(s => !s.IsEmpty).Select(s => s.ItemId);
    }
    
    private static bool IsValidItemForSlot(EquipmentSlotType slotType, ItemType itemType)
    {
        return slotType switch
        {
            EquipmentSlotType.Head or 
            EquipmentSlotType.Chest or 
            EquipmentSlotType.Legs or 
            EquipmentSlotType.Feet or 
            EquipmentSlotType.Hands => itemType == ItemType.Armor,
            
            EquipmentSlotType.MainHand or 
            EquipmentSlotType.OffHand => itemType == ItemType.Weapon || itemType == ItemType.Armor, // Escudo conta como armor
            
            EquipmentSlotType.Accessory1 or 
            EquipmentSlotType.Accessory2 or 
            EquipmentSlotType.Accessory3 => itemType == ItemType.Accessory,
            
            _ => false
        };
    }
}

/// <summary>
/// Slot de equipamento do personagem - Entidade persistível.
/// </summary>
public class EquipmentSlot
{
    public int Id { get; set; }
    public int EquipmentsId { get; set; }
    public EquipmentSlotType SlotType { get; set; }
    public int ItemId { get; set; }
    
    public bool IsEmpty => ItemId <= 0;
    
    public EquipmentSlot() { }
    
    public EquipmentSlot(EquipmentSlotType slotType, int itemId = 0)
    {
        SlotType = slotType;
        ItemId = itemId;
    }
}
    
public enum EquipmentSlotType : byte
{
    Head = 0,
    Chest = 1,
    Legs = 2,
    Feet = 3,
    Hands = 4,
    MainHand = 5,
    OffHand = 6,
    Accessory1 = 7,
    Accessory2 = 8,
    Accessory3 = 9
}

/// <summary>
/// Resultado de uma operação de equipamento.
/// </summary>
public readonly record struct EquipmentResult(
    bool IsSuccess,
    int? UnequippedItemId,
    string? Message = null)
{
    public static EquipmentResult Success(int? unequippedItemId = null) => new(true, unequippedItemId);
    public static EquipmentResult Fail(string message) => new(false, null, message);
}

/// <summary>
/// Bônus de combate direto dos equipamentos.
/// </summary>
public readonly record struct EquipmentCombatBonus(
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense,
    float AttackSpeed,
    float MovementSpeed)
{
    public static EquipmentCombatBonus Zero => default;
}