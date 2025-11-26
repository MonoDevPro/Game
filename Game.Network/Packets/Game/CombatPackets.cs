using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct CombatStateSnapshot(
    int AttackerNetworkId,
    AttackStyle Style,
    float AttackDuration,
    float CooldownRemaining);

/// <summary>
/// Server -> Client: estado de combate do atacante (para iniciar animação).
/// </summary>
[MemoryPackable]
public readonly partial record struct CombatStatePacket(CombatStateSnapshot[] CombatStates);