using System.ComponentModel.DataAnnotations.Schema;
using Game.Domain.AOI.ValueObjects;
using Game.Domain.Player.ValueObjects;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Commons;
using Game.Domain.Commons.Entities;
using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Attributes;
using Game.Domain.Commons.ValueObjects.Character;
using Game.Domain.Commons.ValueObjects.Equipment;
using Game.Domain.Commons.ValueObjects.Identitys;
using Game.Domain.Commons.ValueObjects.Map;
using Game.Domain.Commons.ValueObjects.Vitals;
using Game.Domain.Vocations.ValueObjects;

namespace Game.Domain.Player;

/// <summary>
/// Agregado de atributos do personagem (persistência + derivados).
/// 
/// Observação: os campos [NotMapped] são DERIVADOS e devem ser atualizados via RefreshDerived().
/// </summary>
public sealed class PlayerSimulationAttributes
{
    // Persistência (EF)
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

    // Derivados (ignorar em EF)
    [NotMapped] public Name Name { get; set; }
    [NotMapped] public Vocation Vocation { get; set; }
    [NotMapped] public Progress Progress { get; set; }
    [NotMapped] public Equipment Equipment { get; set; }
    [NotMapped] public BaseStats Stats { get; set; }
    [NotMapped] public CombatStats CombatStats { get; set; }
    [NotMapped] public Health Hp { get; set; }
    [NotMapped] public Mana Mp { get; set; }
    [NotMapped] public GridPosition Position { get; set; }
    [NotMapped] public PlayerOwnership Ownership { get; set; }
    [NotMapped] public VisibilityConfig VisibilityConfig { get; set; } = VisibilityConfig.ForPlayer;

    // Derivados simples (UI/debug)
    [NotMapped] public int MaxHp { get; private set; }
    [NotMapped] public int MaxMp { get; private set; }
    [NotMapped] public int PhysicalAttack { get; private set; }
    [NotMapped] public int MagicAttack { get; private set; }
    [NotMapped] public int PhysicalDefense { get; private set; }
    [NotMapped] public int MagicDefense { get; private set; }
    [NotMapped] public int AttackSpeedPermille { get; private set; }

    // Parameterless ctor (EF)
    public PlayerSimulationAttributes()
    {
        Name = Name.Create(string.Empty);
        Vocation = Vocation.Create(VocationType.None);
        Progress = Progress.Create(level: GameConstants.Character.BASE_LEVEL, experience: 0);
        Equipment = Equipment.CreateFromEntity([]);
        Stats = new BaseStats(Strength: 1, Dexterity: 1, Intelligence: 1, Constitution: 1, Spirit: 1);

        CombatStats = CombatStats.BuildFrom(Stats, Vocation, Progress.Level);
        Hp = Health.Create(Stats, Progress);
        Mp = Mana.Create(Stats, Progress);

        Position = new GridPosition(0, 0);
        Ownership = new PlayerOwnership();

        RefreshDerived();
    }

    public PlayerSimulationAttributes(
        PlayerOwnership ownership,
        Name name,
        Vocation vocation,
        Progress progress,
        Equipment equipment,
        BaseStats totalStats,
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
        Stats = totalStats;
        CombatStats = combatStats;
        Hp = hp;
        Mp = mp;
        Position = gridPosition;

        RefreshDerived();
    }

    /// <summary>
    /// Atualiza os campos derivados (NotMapped).
    /// Chame sempre que Stats/Progress/Vocation/CombatStats/Hp/Mp mudarem.
    /// </summary>
    public void RefreshDerived()
    {
        Level = Progress.Level;
        Experience = Progress.Experience;

        BaseStrength = Stats.Strength;
        BaseDexterity = Stats.Dexterity;
        BaseIntelligence = Stats.Intelligence;
        BaseConstitution = Stats.Constitution;
        BaseSpirit = Stats.Spirit;

        CurrentHp = Hp.Current;
        CurrentMp = Mp.Current;

        MaxHp = Hp.Maximum;
        MaxMp = Mp.Maximum;

        PhysicalAttack = CombatStats.PhysicalAttack;
        MagicAttack = CombatStats.MagicAttack;
        PhysicalDefense = CombatStats.PhysicalDefense;
        MagicDefense = CombatStats.MagicDefense;
        AttackSpeedPermille = CombatStats.AttackSpeedPermille;
    }

    /// <summary>
    /// Cria a partir da entidade Character (persistência), calculando derivados.
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
        var progress = Progress.Create(level: character.Level, experience: character.Experience);

        var equipment = Equipment.CreateFromEntity(character.Equipments.GetAllEquippedItemIds());

        var totalStats = new BaseStats(
            Strength: character.Strength,
            Dexterity: character.Dexterity,
            Intelligence: character.Intelligence,
            Constitution: character.Constitution,
            Spirit: character.Spirit);

        var combatStats = CombatStats.BuildFrom(totalStats, vocation, progress.Level);

        var hp = Health.Create(total: totalStats, progress: progress, current: character.Stats.CurrentHp);
        var mp = Mana.Create(total: totalStats, progress: progress, current: character.Stats.CurrentMp);

        var position = new GridPosition(character.PositionX, character.PositionY);

        var attrs = new PlayerSimulationAttributes(
            ownership,
            name,
            vocation,
            progress,
            equipment,
            totalStats,
            combatStats,
            hp,
            mp,
            position);

        attrs.CharacterId = character.Id;
        attrs.RefreshDerived();
        return attrs;
    }

    /// <summary>
    /// Recalcula atributos mantendo HP/MP atuais (útil para level up ou mudança de equipamentos).
    /// </summary>
    public PlayerSimulationAttributes Recalculate(
        Progress? newProgress = null,
        BaseStats? newBase = null)
    {
        Progress = newProgress ?? Progress;
        Stats = newBase ?? Stats;

        // Sempre reconstroi derivados a partir do estado atual (bug fix)
        CombatStats = CombatStats.BuildFrom(Stats, Vocation, Progress.Level);
        Hp = Health.Create(Stats, Progress, Hp.Current);
        Mp = Mana.Create(Stats, Progress, Mp.Current);

        RefreshDerived();
        return this;
    }

    public int HpRegenPerTick() => Hp.RegenPerTick;
    public int MpRegenPerTick() => Mp.RegenPerTick;
}