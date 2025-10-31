using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct PlayerStatePacket(
    int NetworkId,
    Position Position,
    Facing Facing);