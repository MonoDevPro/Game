using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct PlayerVitalsPacket(
    int PlayerId,
    Health Health,
    Mana Mana);
