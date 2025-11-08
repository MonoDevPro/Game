using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

/// <summary>
/// Server -> Client: estado de combate do atacante (para iniciar animação).
/// </summary>
[MemoryPackable]
public readonly partial record struct CombatStatePacket(
    int AttackerNetworkId,
    int DefenderNetworkId,
    AttackType Type,
    float AttackDuration,
    float CooldownRemaining);

/// <summary>
/// Server -> Client: resultado do ataque (hit, dano, crítico).
/// </summary>
[MemoryPackable]
public readonly partial record struct AttackResultPacket(
    int AttackerNetworkId,
    int DefenderNetworkId,
    int Damage,
    bool WasHit,
    bool IsCritical,
    AttackType AnimationType,
    float TimeToLive);