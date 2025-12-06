using Game.ECS.Schema.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct AttackData(
    int AttackerNetworkId,
    AttackStyle Style,
    float AttackDuration,
    float CooldownRemaining);

[MemoryPackable]
public readonly partial record struct AttackPacket(AttackData[] Attacks);
