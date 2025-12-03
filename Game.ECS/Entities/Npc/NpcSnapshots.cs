namespace Game.ECS.Entities.Npc;

/// <summary>
/// Snapshot completo de um NPC (usado no spawn).
/// </summary>
public readonly record struct NpcSnapshot(
    int NetworkId,
    int MapId,
    string Name,
    int X,
    int Y,
    sbyte Floor,
    sbyte DirX,
    sbyte DirY,
    int Hp,
    int MaxHp,
    int Mp,
    int MaxMp);

/// <summary>
/// Snapshot de estado de um NPC (posição e movimento).
/// </summary>
public readonly record struct NpcStateSnapshot(
    int NetworkId,
    int X,
    int Y,
    sbyte Floor,
    float Speed,
    sbyte DirectionX,
    sbyte DirectionY);

/// <summary>
/// Snapshot de vitals de um NPC (HP/MP).
/// </summary>
public readonly record struct NpcVitalsSnapshot(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp);
