using Game.Domain.VOs;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets.Simulation;

/// <summary>
/// Client -> Server: Conecta ao jogo com token (CONNECTED).
/// Autor: MonoDevPro
/// Data: 2025-01-12 06:37:40
/// </summary>
[MemoryPackable]
public readonly partial record struct GameConnectPacket(string GameToken) : IPacket;

/// <summary>
/// Client -> Server player input payload (grid movement and action flags).
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerInputPacket(
    uint Sequence,
    DirectionOffset Movement,
    DirectionOffset MouseLook,
    ushort Buttons) : IPacket;