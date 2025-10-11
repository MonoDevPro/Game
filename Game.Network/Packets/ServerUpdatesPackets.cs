using Game.Domain.VOs;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client player movement state update (position and facing).
/// Sent at high frequency (60Hz) using Sequenced delivery.
/// Autor: MonoDevPro
/// Data: 2025-10-11 01:09:48
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerMovementPacket(
    int NetworkId, Coordinate Position, Coordinate Facing) : IPacket;

/// <summary>
/// Server -> Client player vitals update (HP/MP).
/// Sent only when values change using ReliableOrdered delivery.
/// Autor: MonoDevPro
/// Data: 2025-10-11 01:09:48
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerVitalsPacket(
    int NetworkId, int CurrentHp, int MaxHp, int CurrentMp, int MaxMp) : IPacket;