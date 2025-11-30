using Game.Domain.Enums;
using Game.ECS.Schema.Components;

namespace Game.ECS.Schema.Templates;

public readonly record struct IdentityTemplate(
    int NetworkId,
    string Name,
    Gender Gender,
    VocationType Vocation
);

public readonly record struct LocationTemplate(
    int MapId,
    int Floor,
    int X,
    int Y
);

public readonly record struct DirectionTemplate(
    sbyte DirX,
    sbyte DirY
);

public readonly record struct StatsTemplate(
    float MovementSpeed,
    float AttackSpeed,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense
);

public readonly record struct VitalsTemplate(
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp,
    float HpRegen,
    float MpRegen
);

public readonly record struct BehaviorTemplate(
    BehaviorType BehaviorType,
    float VisionRange,
    float AttackRange,
    float LeashRange,
    float PatrolRadius,
    float IdleDurationMin,
    float IdleDurationMax
);