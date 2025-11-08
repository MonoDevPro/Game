using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct LeftPacket(
    int NetworkId);