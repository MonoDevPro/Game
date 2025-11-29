namespace Game.ECS.Entities.Npc;

/// <summary>
/// Dados de um NPC (controlado por IA).
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
    int MaxMp,
    byte Vocation = 0,
    byte Gender = 0,
    float MovementSpeed = 1.0f,
    float AttackSpeed = 1.0f
);

public readonly record struct NpcStateSnapshot(
    int NetworkId, 
    int X, 
    int Y, 
    sbyte Floor, 
    float Speed, 
    sbyte DirectionX, 
    sbyte DirectionY
);

public readonly record struct NpcVitalsSnapshot(
    int NetworkId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp
);