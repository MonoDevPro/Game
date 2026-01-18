using Game.DTOs.Map;
using MemoryPack;

namespace Game.ECS.Services.Snapshot.Data;

[MemoryPackable]
public readonly partial record struct PlayerData(
    int PlayerId, int NetworkId, int MapId,
    string Name, byte Gender, byte Vocation,
    int X, int Y, int Z,
    int DirX, int DirY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense
);
    
[MemoryPackable]
public readonly partial record struct PlayerSpawnPacket(PlayerData[] PlayerData);