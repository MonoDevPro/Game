using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct StatePacket(
    int NetworkId,
    Position Position,
    Velocity Velocity,
    Facing Facing);