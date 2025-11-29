namespace Game.ECS.Entities.Npc;

public readonly record struct NpcSpawnPoint(
    int NetworkId,
    int MapId,
    int Floor,
    int X,
    int Y
);

public class NpcTemplate
{
    public int NetworkId { get; set; }
    public int NpcId { get; set; }
    public string Name { get; set; } = null!;
    public byte Gender { get; set; }
    public byte Vocation { get; set; }
    
    public NpcStats Stats { get; set; }
    public NpcVitals Vitals { get; set; }
    public NpcBehaviorConfig Behavior { get; set; }
}

public readonly record struct NpcStats(
    float MovementSpeed,
    float AttackSpeed,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense
);

public readonly record struct NpcVitals(
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp,
    float HpRegen,
    float MpRegen
);

public enum NpcBehaviorType
{
    Passive,
    Aggressive,
    Neutral
}

public readonly record struct NpcBehaviorConfig(
    NpcBehaviorType BehaviorType,
    float VisionRange,
    float AttackRange,
    float LeashRange,
    float PatrolRadius,
    float IdleDurationMin,
    float IdleDurationMax
);