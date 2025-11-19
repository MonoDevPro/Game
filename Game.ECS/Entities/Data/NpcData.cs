namespace Game.ECS.Entities.Data;

/// <summary>
/// Dados de um NPC (controlado por IA).
/// </summary>
public readonly record struct NPCData(
    int NetworkId,
    int PositionX, int PositionY, int PositionZ,
    int Hp, int MaxHp, float HpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense,
    int MapId = 0);

// ============================================
// NPC Snapshots
// ============================================

public readonly record struct NpcStateData(
    int NetworkId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int FacingX,
    int FacingY,
    float Speed,
    int CurrentHp,
    int MaxHp);
