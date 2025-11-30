using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Entities.Player;

/// <summary>
/// Dados completos de um jogador (usado no spawn).
/// </summary>
public readonly record struct PlayerSnapshot(
    int PlayerId, int NetworkId, int MapId,
    string Name, byte GenderId, byte VocationId,
    int PosX, int PosY, sbyte Floor,
    sbyte DirX, sbyte DirY,
    int Hp, int MaxHp,
    int Mp, int MaxMp,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense);

public readonly record struct StateSnapshot(
    int NetworkId,
    int PosX,
    int PosY,
    sbyte Floor,
    float Speed,
    sbyte DirX,
    sbyte DirY
);

public readonly record struct VitalsSnapshot(
    int NetworkId, 
    int Hp, 
    int MaxHp, 
    int Mp, 
    int MaxMp);