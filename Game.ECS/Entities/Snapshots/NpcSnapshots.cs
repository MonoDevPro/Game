using Game.Domain.Enums;
using Game.ECS.Schema.Components;

namespace Game.ECS.Schema.Snapshots;

public readonly record struct Behaviour(
    BehaviorType BehaviorType,
    float VisionRange,
    float AttackRange,
    float LeashRange,
    float PatrolRadius,
    float IdleDurationMin,
    float IdleDurationMax
)
{
    public static readonly Behaviour Default = new(
        BehaviorType.Passive,
        VisionRange: 5f,
        AttackRange: 1.5f,
        LeashRange: 10f,
        PatrolRadius: 3f,
        IdleDurationMin: 2f,
        IdleDurationMax: 5f
    );
}

/// <summary>
/// Snapshot completo de um NPC (usado no spawn).
/// </summary>
public readonly record struct NpcSnapshot(
    int NpcId,
    int NetworkId,
    int MapId,
    string Name,
    byte GenderId,
    byte VocationId,
    int PosX,
    int PosY,
    sbyte Floor,
    sbyte DirX,
    sbyte DirY,
    int Hp,
    int MaxHp,
    float HpRegen,
    int Mp,
    int MaxMp,
    float MpRegen,
    float MovementSpeed,
    float AttackSpeed,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense)
{
    public Gender Gender => (Gender)GenderId;
    public VocationType Vocation => (VocationType)VocationId;
}
