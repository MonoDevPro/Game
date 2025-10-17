using Game.Network.Abstractions;
using Game.Network.Packets.DTOs;
using MemoryPack;

namespace Game.Network.Packets.Simulation;

[MemoryPackable]
public readonly partial struct GameDataPacket : IPacket
{
    public MapData MapData { get; init; }
    public PlayerSnapshot LocalPlayer { get; init; }
    public PlayerSnapshot[] OtherPlayers { get; init; }
}

[MemoryPackable]
public readonly partial record struct PlayerSpawnPacket(PlayerSnapshot Player) : IPacket;

/// <summary>
/// Server -> Client notification when a player leaves the world.
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerDespawnPacket(int NetworkId) : IPacket;

/// <summary>
/// Server -> Client player movement state update (position, facing, and speed).
/// Sent at high frequency (60Hz) using Sequenced delivery.
/// Autor: MonoDevPro
/// Data: 2025-10-11 01:09:48
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerMovementPacket(
    int NetworkId,
    int PositionX,
    int PositionY,
    int PositionZ,
    int FacingX,
    int FacingY,
    float Speed
) : IPacket;

/// <summary>
/// Server -> Client player vitals update (HP/MP).
/// Sent only when values change using ReliableOrdered delivery.
/// Autor: MonoDevPro
/// Data: 2025-10-11 01:09:48
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerVitalsPacket(
    int NetworkId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp
) : IPacket;