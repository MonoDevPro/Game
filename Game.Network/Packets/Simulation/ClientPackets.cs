using Game.Domain.VOs;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets.Simulation;

/// <summary>
/// Client -> Server player input payload (grid movement and action flags).
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerInputPacket(
    GridOffset Movement,
    GridOffset MouseLook,
    ushort Buttons) : IPacket;