using System;
using Game.Domain.AOI.ValueObjects;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.Player.ValueObjects;
using Game.Domain.ValueObjects.Character;
using Game.Domain.ValueObjects.Attributes;
using Game.Domain.ValueObjects.Equipment;
using Game.Domain.ValueObjects.Identitys;
using Game.Domain.ValueObjects.Map;
using Game.Domain.ValueObjects.Vitals;
using System.ComponentModel.DataAnnotations.Schema;

namespace Game.Domain.Player;

/// <summary>
/// Agregado completo de atributos do personagem.
/// Imutável e pronto para uso na camada ECS.
/// </summary>
public sealed class PlayerSimulationAttributes
{
    // Persistência
    public int Id { get; set; }
    public int CharacterId { get; set; }
    public Character? Character { get; set; }
    public int Level { get; set; }
    public long Experience { get; set; }
    public int BaseStrength { get; set; }
    public int BaseDexterity { get; set; }
    public int BaseIntelligence { get; set; }
    public int BaseConstitution { get; set; }
    public int BaseSpirit { get; set; }
    public int CurrentHp { get; set; }
    public int CurrentMp { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }

    // Calculados (ignorar em EF)
    [NotMapped] public int TotalStrength { get; set; }
    [NotMapped] public int TotalDexterity { get; set; }
    [NotMapped] public int TotalIntelligence { get; set; }
    [NotMapped] public int TotalConstitution { get; set; }
    [NotMapped] public int TotalSpirit { get; set; }
    [NotMapped] public int BonusStrength { get; set; }
    [NotMapped] public int BonusDexterity { get; set; }
    [NotMapped] public int BonusIntelligence { get; set; }
    [NotMapped] public int BonusConstitution { get; set; }
    [NotMapped] public int BonusSpirit { get; set; }
    [NotMapped] public int MaxHp { get; set; }
    [NotMapped] public int MaxMp { get; set; }
    [NotMapped] public double PhysicalAttack { get; set; }
    [NotMapped] public double MagicAttack { get; set; }
    [NotMapped] public double PhysicalDefense { get; set; }
    [NotMapped] public double MagicDefense { get; set; }
    [NotMapped] public double AttackSpeed { get; set; }
    [NotMapped] public double MovementSpeed { get; set; }

    public Name Name { get; }
    public Vocation Vocation { get; }
    public Progress Progress { get; private set; }
    public Equipment Equipment { get; private set; }
    public BaseStats Stats { get; private set; }
    public CombatStats CombatStats { get; private set; }
    public Health Hp { get; private set; }
    public Mana Mp { get; private set; }
    public GridPosition Position { get; private set; }
    public PlayerOwnership Ownership { get; private set; }
    public VisibilityConfig VisibilityConfig { get; private set; } = VisibilityConfig.ForPlayer;

    private PlayerSimulationAttributes(
        PlayerOwnership ownership,
        Name name,
        Vocation vocation,
        Progress progress,
        Equipment equipment,
        BaseStats baseStats,
        CombatStats combatStats,
        Health hp,
        Mana mp,
        GridPosition gridPosition)
    {
        Ownership = ownership;
        Name = name;
        Vocation = vocation;
        Progress = progress;
        Equipment = equipment;
        Stats = baseStats;
        CombatStats = combatStats;
        Hp = hp;
        Mp = mp;
        Position = gridPosition;
    }

    /// <summary>
    /// Cria um novo CharacterAttributes calculando todos os valores derivados.
    /// </summary>
    public static PlayerSimulationAttributes Create(Character character)
    {
        var ownership = new PlayerOwnership
        {
            AccountId = character.AccountId,
            CharacterId = character.Id
        };
        
        var name = Name.Create(character.Name);
        
        var vocation = Vocation.Create(character.Vocation);
        
        var progress = Progress.Create(
            level: character.Level,
            experience: character.Experience);

        var equipment = Equipment.CreateFromEntity(
            character.Equipments.GetAllEquippedItemIds());
        
        var totalStats = new BaseStats(
            Strength: character.Strength,
            Dexterity: character.Dexterity,
            Intelligence: character.Intelligence,
            Constitution: character.Constitution,
            Spirit: character.Spirit);
        
        var combatStats = CombatStats.BuildFrom(totalStats, vocation);
        
        var hp = Health.Create(
            total: totalStats, 
            progress: progress, 
            current: character.Stats.CurrentHp);
        
        var mp = Mana.Create(
            total: totalStats, 
            progress: progress,
            current: character.Stats.CurrentMp);
        
        var position = new GridPosition(
            character.PositionX,
            character.PositionY);
        
        var attrs = new PlayerSimulationAttributes(
            ownership,
            name: name,
            vocation: vocation,
            progress: progress,
            equipment: equipment,
            baseStats: totalStats,
            combatStats: combatStats,
            hp: hp,
            mp: mp,
            position);
        
        // popular campos de persistência
        attrs.CharacterId = character.Id;
        attrs.Level = progress.Level;
        attrs.Experience = progress.Experience;
        attrs.BaseStrength = (int)totalStats.Strength;
        attrs.BaseDexterity = (int)totalStats.Dexterity;
        attrs.BaseIntelligence = (int)totalStats.Intelligence;
        attrs.BaseConstitution = (int)totalStats.Constitution;
        attrs.BaseSpirit = (int)totalStats.Spirit;
        attrs.CurrentHp = (int)hp.Current;
        attrs.CurrentMp = (int)mp.Current;
        attrs.MaxHp = (int)hp.Maximum;
        attrs.MaxMp = (int)mp.Maximum;
        attrs.PhysicalAttack = combatStats.PhysicalAttack;
        attrs.MagicAttack = combatStats.MagicAttack;
        attrs.PhysicalDefense = combatStats.PhysicalDefense;
        attrs.MagicDefense = combatStats.MagicDefense;
        attrs.AttackSpeed = combatStats.AttackSpeed;
        attrs.MovementSpeed = combatStats.AttackSpeed; // placeholder

        return attrs;
    }

    /// <summary>
    /// Recalcula atributos mantendo HP/MP atuais (útil para level up ou mudança de equipamentos).
    /// </summary>
    public PlayerSimulationAttributes Recalculate(
        Progress? newProgress = null,
        BaseStats? newBase = null,
        Health? newHp = null,
        Mana? newMp = null)
    {
        Progress = newProgress ?? Progress;
        Stats = newBase ?? Stats;
        CombatStats = newBase is null ? CombatStats.BuildFrom(Stats, Vocation) : CombatStats;
        Hp = newHp ?? Health.Create(Stats, Progress, Hp.Current);
        Mp = newMp ?? Mana.Create(Stats, Progress, Mp.Current);
        MaxHp = (int)Hp.Maximum;
        MaxMp = (int)Mp.Maximum;
        return this;
    }

    public int HpRegenPerTick() => (int)Hp.RegenPerTick;
    public int MpRegenPerTick() => (int)Mp.RegenPerTick;
}
