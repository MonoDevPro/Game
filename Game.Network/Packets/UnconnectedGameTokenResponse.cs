using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client: Token de jogo para conectar (unconnected).
/// Autor: MonoDevPro
/// Data: 2025-01-12 06:37:40
/// </summary>
[MemoryPackable]
public readonly partial record struct UnconnectedGameTokenResponsePacket(string GameToken) : IPacket;
