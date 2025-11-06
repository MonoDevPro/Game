using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct VitalsPacket(
    int NetworkId,
    Health Health,
    Mana Mana);
