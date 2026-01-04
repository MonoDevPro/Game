using System.ComponentModel.DataAnnotations.Schema;
using Game.Domain.Enums;

namespace Game.Domain.Entities;

/// <summary>
/// Personagem jogável
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public sealed class Character : BaseEntity
{
    public string Name { get; init; } = null!;
    public Gender Gender { get; set; } = Gender.Unknown;
    public VocationType Vocation { get; set; } = VocationType.Unknown;
    
    // Posição no mundo
    public int MapId { get; set; }
    public int DirX { get; set; }
    public int DirY { get; set; }
    public int PosX { get; set; }
    public int PosY { get; set; }
    public int PosZ { get; set; }
    
    // Atributos
    public int Level { get; set; } = 1;
    public long Experience { get; set; } = 0;
    
    public int BaseStrength { get; set; } = 5;
    public int BaseDexterity { get; set; } = 5;
    public int BaseIntelligence { get; set; } = 5;
    public int BaseConstitution { get; set; } = 5;
    public int BaseSpirit { get; set; } = 5;
    
    public int CurrentHp { get; set; } = 50;
    public int CurrentMp { get; set; } = 30;
    
    public void CreateInitial(
        VocationType vocationType,
        int defaultLevel = 1,
        int defaultInventoryCapacity = 30,
        int defaultSpawnX = 5,
        int defaultSpawnY = 5,
        int defaultSpawnZ = 0)
    {
        Vocation = vocationType;
        Level = defaultLevel;
        Experience = 0;
        
        // Stats base variam por vocação
        (BaseStrength, BaseDexterity, BaseIntelligence, 
            BaseConstitution, BaseSpirit) = Vocation switch
        {
            VocationType.Warrior => (15, 10, 5, 12, 8),
            VocationType.Archer => (10, 15, 7, 10, 10),
            VocationType.Mage => (5, 8, 15, 8, 12),
            _ => (10, 10, 10, 10, 10)
        };
        
        CurrentHp = MaxHp;
        CurrentMp = MaxMp;
        
        Inventory = new Inventory
        {
            CharacterId = Id,
            Character = this,
            Capacity = defaultInventoryCapacity
        };
        
        DirX = 0;
        DirY = 1;
        PosX = defaultSpawnX;
        PosY = defaultSpawnY;
        PosZ = defaultSpawnZ;
    }
    
    // Atributos Totais (Base + Bônus de Equipamento) [NotMapped - calculados]
    [NotMapped] public int TotalStrength => BaseStrength + BonusStrength;
    
    [NotMapped] public int TotalDexterity => BaseDexterity + BonusDexterity;
    
    [NotMapped] public int TotalIntelligence => BaseIntelligence + BonusIntelligence;
    
    [NotMapped] public int TotalConstitution => BaseConstitution + BonusConstitution;
    
    [NotMapped] public int TotalSpirit => BaseSpirit + BonusSpirit;
    
    // Bônus de Equipamento (calculados externamente, não persistidos)
    [NotMapped] public int BonusStrength { get; set; }
    
    [NotMapped] public int BonusDexterity { get; set; }
    
    [NotMapped] public int BonusIntelligence { get; set; }
    
    [NotMapped]
    public int BonusConstitution { get; set; }
    
    [NotMapped]
    public int BonusSpirit { get; set; }
    
    // Stats Derivados (calculados com base nos totais)
    [NotMapped]
    public int MaxHp => 10 * TotalConstitution + Level * 5;
    
    [NotMapped]
    public int MaxMp => 5 * TotalIntelligence + Level * 3;
    
    [NotMapped]
    public int PhysicalAttack => TotalStrength * 2 + Level;
    
    [NotMapped]
    public int MagicAttack => TotalIntelligence * 3 + (TotalSpirit / 2);
    
    [NotMapped]
    public int PhysicalDefense => TotalConstitution + (TotalStrength / 2);
    
    [NotMapped]
    public int MagicDefense => TotalSpirit + (TotalIntelligence / 2);
    
    [NotMapped]
    public double AttackSpeed => 1.0 + (TotalDexterity / 100.0);
    
    [NotMapped]
    public double MovementSpeed => 1.0 + (TotalDexterity / 200.0);
    
    // Regeneração
    public int HpRegenPerTick() => Math.Max(1, TotalConstitution / 10);
    public int MpRegenPerTick() => Math.Max(1, TotalSpirit / 10);
    
    // Relacionamentos
    public int AccountId { get; init; }
    public Account Account { get; set; } = null!;
    
    // Um personagem tem um inventário (1:1)
    public Inventory Inventory { get; set; } = null!;
    
    // Um personagem tem múltiplos slots de equipamento (1:N)
    public ICollection<EquipmentSlot> Equipment { get; init; } = new List<EquipmentSlot>();
    
    public override string ToString() => $"Character(Id={Id}, Name={Name}, Vocation={Vocation}, Level={Level}, Pos=({PosX},{PosY},{PosZ}))";
    
}