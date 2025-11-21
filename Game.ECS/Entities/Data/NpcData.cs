namespace Game.ECS.Entities.Data;

/// <summary>
/// Dados de um NPC (controlado por IA).
/// </summary>
public readonly record struct NPCData(
    int NetworkId, string Name,
    byte Gender, byte Vocation,
    int PositionX, int PositionY, int PositionZ,
    int FacingX, int FacingY,
    int Hp, int MaxHp, float HpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense,
    int MapId = 0);

public readonly record struct NpcBehaviorData(
    byte BehaviorType,
    float VisionRange,
    float AttackRange,
    float LeashRange,
    float PatrolRadius,
    float IdleDurationMin,
    float IdleDurationMax);

// ============================================
// NPC Snapshots
// ============================================

public readonly record struct NpcStateData(
    int NetworkId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int VelocityX,
    int VelocityY,
    float Speed,
    int FacingX,
    int FacingY);

public readonly record struct NpcHealthData(
    int NetworkId,
    int CurrentHp,
    int MaxHp);


