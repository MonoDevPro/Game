using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

/// <summary>
/// Server -> Client: estado de combate do atacante (para iniciar animação).
/// </summary>
[MemoryPackable]
public readonly partial record struct CombatStatePacket(
    int AttackerNetworkId,
    AttackType Type,
    float AttackDuration,
    float CooldownRemaining);

[MemoryPackable]
public readonly partial record struct PlayerDamagedPacket(
    int VictimNetworkId,
    int DamageAmount,
    float TimeToLive);