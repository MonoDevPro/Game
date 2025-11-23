namespace Game.ECS.Entities.Data;

/// <summary>
/// Dados completos de um jogador (usado no spawn).
/// </summary>
public readonly record struct PlayerData(
    int PlayerId, int NetworkId,
    string Name, byte Gender, byte Vocation,
    int PosX, int PosY, sbyte Floor,
    sbyte FacingX, sbyte FacingY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense,
    int MapId = 0);

public readonly record struct PlayerStateData(
    int NetworkId,
    int PositionX,
    int PositionY,
    sbyte Floor,
    sbyte VelocityX,
    sbyte VelocityY,
    float Speed,
    sbyte FacingX,
    sbyte FacingY);

public readonly record struct PlayerVitalsData(
    int NetworkId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp);
