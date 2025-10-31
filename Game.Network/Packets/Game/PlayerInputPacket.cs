using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct PlayerInputPacket(
    sbyte InputX,
    sbyte InputY,
    InputFlags Flags);