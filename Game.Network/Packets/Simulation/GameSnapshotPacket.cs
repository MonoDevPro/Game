using System.Runtime.InteropServices;
using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Simulation;

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct GameSnapshotPacket(
    MapSnapshot MapSnapshot,
    PlayerSnapshot LocalPlayer,
    PlayerSnapshot[] OtherPlayers);
    
/// <summary>
/// Flat representation of a player's visible state for sync packets.
/// </summary>
[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct PlayerSnapshot(
    int NetworkId,
    int PlayerId,
    int CharacterId,
    string Name,
    byte Gender,
    byte Vocation,
    int PositionX,
    int PositionY,
    int PositionZ,
    int FacingX,
    int FacingY,
    float Speed,
    int Hp,
    int Mp,
    int MaxHp,
    int MaxMp,
    float HpRegen,
    float MpRegen,
    int PhysicalAttack,
    int MagicAttack,
    int PhysicalDefense,
    int MagicDefense,
    double AttackSpeed,
    double MovementSpeed);