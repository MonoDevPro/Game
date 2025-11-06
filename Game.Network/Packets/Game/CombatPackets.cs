using Game.ECS.Components;
using MemoryPack;

namespace Game.Network.Packets.Game;

/// <summary>
/// Server -> Client: Notifica um ataque que ocorreu para sincronizar animação.
/// </summary>
[MemoryPackable]
public readonly partial record struct AttackPacket(
    int AttackerNetworkId,      // Quem atacou
    int DefenderNetworkId,       // Quem recebeu o ataque
    int Damage,                  // Dano causado
    bool WasHit,                 // Se acertou ou errou
    float AttackDuration,        // Duração da animação de ataque em segundos
    AttackAnimationType AnimationType = AttackAnimationType.Basic);