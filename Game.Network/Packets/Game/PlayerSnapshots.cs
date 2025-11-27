using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct PlayerSpawn(
    int PlayerId, int NetworkId, int MapId,
    string Name, byte Gender, byte Vocation,
    int X, int Y, sbyte Floor,
    sbyte DirX, sbyte DirY,
    int Hp, int MaxHp,
    int Mp, int MaxMp,
    float MovementSpeed, float AttackSpeed,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense);
    
[MemoryPackable]
public readonly partial record struct PlayerStateUpdate(
    int NetworkId,
    int X, int Y, sbyte Floor,
    float Speed,
    sbyte DirX, sbyte DirY);
    
[MemoryPackable]
public readonly partial record struct PlayerVitalsUpdate(
    int NetworkId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp);