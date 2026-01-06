using System.ComponentModel.DataAnnotations.Schema;
using Game.Domain.Data;
using Game.Domain.Enums;

namespace Game.Domain.Entities;

/// <summary>
/// Personagem jogável
/// Autor: MonoDevPro
/// Data: 2025-10-05 21:10:27
/// </summary>
public sealed class Character : BaseEntity
{
    private const int MinCharacterNameLength = 3;
    private const int MaxCharacterNameLength = 20;

    // Construtor para o EF (materialização/seed via HasData)
    private Character() { }

    public Character(int accountId, string name, Gender gender, VocationType vocation)
    {
        ValidateCharacter(name, vocation, gender);

        AccountId = accountId;
        Name = name;
        Gender = gender;
        Vocation = vocation;

        Initialize(
            defaultLevel: 1,
            defaultInventoryCapacity: 30,
            defaultMapId: 1,
            defaultSpawnX: 5,
            defaultSpawnY: 5,
            defaultSpawnZ: 0);
    }

    // Factory para seed: NÃO chama Initialize() e NÃO seta navegações
    public static Character CreateSeed(
        int id,
        int accountId,
        string name,
        Gender gender,
        VocationType vocation,
        int mapId = 1,
        int posX = 5,
        int posY = 5,
        int posZ = 0,
        int dirX = 0,
        int dirY = 1,
        int level = 1,
        long experience = 0,
        int currentHp = 50,
        int currentMp = 30)
    {
        var c = new Character
        {
            Id = id,

            AccountId = accountId,
            Name = name,
            Gender = gender,
            Vocation = vocation,

            MapId = mapId,
            PosX = posX,
            PosY = posY,
            PosZ = posZ,
            DirX = dirX,
            DirY = dirY,

            Level = level,
            Experience = experience,

            CurrentHp = currentHp,
            CurrentMp = currentMp,

            IsActive = true
        };

        // Base stats (sem Inventory)
        (c.BaseStrength, c.BaseDexterity, c.BaseIntelligence, c.BaseConstitution, c.BaseSpirit) = vocation switch
        {
            VocationType.Warrior => (15, 10, 5, 12, 8),
            VocationType.Archer => (10, 15, 7, 10, 10),
            VocationType.Mage => (5, 8, 15, 8, 12),
            _ => (10, 10, 10, 10, 10)
        };

        return c;
    }

    public string Name { get; init; } = null!;
    public Gender Gender { get; init; }
    public VocationType Vocation { get; init; }

    public int MapId { get; private set; }
    public int DirX { get; private set; }
    public int DirY { get; private set; }
    public int PosX { get; private set; }
    public int PosY { get; private set; }
    public int PosZ { get; private set; }

    public int Level { get; private set; }
    public long Experience { get; private set; }

    public int BaseStrength { get; private set; }
    public int BaseDexterity { get; private set; }
    public int BaseIntelligence { get; private set; }
    public int BaseConstitution { get; private set; }
    public int BaseSpirit { get; private set; }

    public int CurrentHp { get; private set; } = 50;
    public int CurrentMp { get; private set; } = 30;

    public int AccountId { get; init; }
    public Account Account { get; init; } = null!;

    public Inventory Inventory { get; private set; } = null!;
    public ICollection<EquipmentSlot> Equipment { get; init; } = new List<EquipmentSlot>();

    private void Initialize(
        int defaultLevel = 1,
        int defaultInventoryCapacity = 30,
        int defaultMapId = 1,
        int defaultSpawnX = 5,
        int defaultSpawnY = 5,
        int defaultSpawnZ = 0)
    {
        if (Level != 0 || Experience != 0)
            throw new InvalidOperationException("Character already initialized.");

        Level = defaultLevel;
        Experience = 0;

        (BaseStrength, BaseDexterity, BaseIntelligence, BaseConstitution, BaseSpirit) = Vocation switch
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

        MapId = defaultMapId;
        DirX = 0;
        DirY = 1;
        PosX = defaultSpawnX;
        PosY = defaultSpawnY;
        PosZ = defaultSpawnZ;
    }

    [NotMapped] public int TotalStrength => BaseStrength + BonusStrength;
    [NotMapped] public int TotalDexterity => BaseDexterity + BonusDexterity;
    [NotMapped] public int TotalIntelligence => BaseIntelligence + BonusIntelligence;
    [NotMapped] public int TotalConstitution => BaseConstitution + BonusConstitution;
    [NotMapped] public int TotalSpirit => BaseSpirit + BonusSpirit;

    [NotMapped] public int BonusStrength { get; set; }
    [NotMapped] public int BonusDexterity { get; set; }
    [NotMapped] public int BonusIntelligence { get; set; }
    [NotMapped] public int BonusConstitution { get; set; }
    [NotMapped] public int BonusSpirit { get; set; }

    [NotMapped] public int MaxHp => 10 * TotalConstitution + Level * 5;
    [NotMapped] public int MaxMp => 5 * TotalIntelligence + Level * 3;
    
    // Regeneração
    public int HpRegenPerTick() => Math.Max(1, TotalConstitution / 10);
    public int MpRegenPerTick() => Math.Max(1, TotalSpirit / 10);
    
    // Stats Derivados (calculados com base nos totais)
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

    private void ValidateCharacter(string name, VocationType vocation = VocationType.Unknown, Gender gender = Gender.Unknown)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Character name cannot be null or empty.", nameof(name));
        if (name.Length is < MinCharacterNameLength or > MaxCharacterNameLength)
            throw new ArgumentException($"Character name must be between {MinCharacterNameLength} and {MaxCharacterNameLength} characters.", nameof(name));
        if (!name.All(c => char.IsLetter(c) || c == ' '))
            throw new ArgumentException("Nome pode conter apenas letras e espaços.");
        if (name.StartsWith(' ') || name.EndsWith(' '))
            throw new ArgumentException("Nome não pode começar ou terminar com espaço.");
        if (name.Contains("  "))
            throw new ArgumentException("Nome não pode conter espaços consecutivos.");
        if (vocation == VocationType.Unknown)
            throw new ArgumentException("Vocation cannot be Unknown.", nameof(vocation));
        if (gender == Gender.Unknown)
            throw new ArgumentException("Gender cannot be Unknown.", nameof(gender));
    }
    
    public void ApplyCharacterState(CharacterState state)
    {
        MapId = state.MapId;
        PosX = state.PositionX;
        PosY = state.PositionY;
        PosZ = state.PositionZ;
        DirX = state.DirX;
        DirY = state.DirY;
        
        CurrentHp = Math.Clamp(state.CurrentHp, 0, MaxHp);
        CurrentMp = Math.Clamp(state.CurrentMp, 0, MaxMp);
    }
    
    public void ApplyStatsState(StatsState stats)
    {
        Level = stats.Level;
        Experience = stats.Experience;
        BaseStrength = stats.BaseStrength;
        BaseDexterity = stats.BaseDexterity;
        BaseIntelligence = stats.BaseIntelligence;
        BaseConstitution = stats.BaseConstitution;
        BaseSpirit = stats.BaseSpirit;
    }
    
    public void ApplyVitalsState(VitalsState vitals)
    {
        CurrentHp = Math.Clamp(vitals.CurrentHp, 0, MaxHp);
        CurrentMp = Math.Clamp(vitals.CurrentMp, 0, MaxMp);
    }
}