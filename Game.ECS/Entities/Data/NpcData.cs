namespace Game.ECS.Entities.Data;

/// <summary>
/// Dados de um NPC (controlado por IA).
/// </summary>
public readonly record struct NPCData(
    int NetworkId,
    string Name,
    byte Gender,
    byte Vocation,
    int PositionX,
    int PositionY,
    sbyte Floor,
    sbyte FacingX,
    sbyte FacingY,
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
    int MagicDefense,
    int MapId = 0)
{
    public string DisplayDebugInfo()
    {
        return $"NPC[NetID={NetworkId}, Name={Name}, Voc={Vocation}, Gender={Gender}, Pos=({PositionX},{PositionY},{Floor}), HP={Hp}/{MaxHp}, MP={Mp}/{MaxMp}]";
    }
}

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
    sbyte Floor,
    sbyte VelocityX,
    sbyte VelocityY,
    float Speed,
    sbyte FacingX,
    sbyte FacingY);

public readonly record struct NpcVitalsData(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp);


