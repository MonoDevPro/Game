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

namespace Game.Domain.Player;

/// <summary>
/// Agregado completo de atributos do personagem.
/// Imutável e pronto para uso na camada ECS.
/// </summary>
public sealed class PlayerSimulationAttributes
{
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
        
        var vocation = Vocation.Create((VocationType)character.Vocation);
        
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
            current: character.Hp);
        
        var mp = Mana.Create(
            total: totalStats, 
            progress: progress);
        
        var position = new GridPosition(
            character.X,
            character.Y);
        
        return new PlayerSimulationAttributes(
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
        return this;
    }
}
