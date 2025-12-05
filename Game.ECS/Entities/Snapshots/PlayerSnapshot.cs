using Game.Domain.Enums;

namespace Game.ECS.Schema.Snapshots;

/// <summary>
/// Dados completos de um jogador (usado no spawn).
/// </summary>
public readonly record struct PlayerSnapshot(
    int PlayerId,
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