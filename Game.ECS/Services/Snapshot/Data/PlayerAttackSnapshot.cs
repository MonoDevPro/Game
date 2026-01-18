using MemoryPack;

namespace Game.ECS.Services.Snapshot.Data;

[MemoryPackable]
public readonly partial record struct PlayerAttackSnapshot(
    int AttackerNetworkId,
    AttackStyle Style,
    float AttackDuration,
    float CooldownRemaining);

[MemoryPackable]
public readonly partial record struct PlayerAttackPacket(PlayerAttackSnapshot[] Attacks);
    
/// <summary>
/// Define o estilo de ataque baseado na vocação.
/// </summary>
public enum AttackStyle : byte
{
    Melee = 0,   // Ataque corpo a corpo (Warriors)
    Ranged = 1,  // Ataque à distância com projétil físico (Archers)
    Magic = 2    // Ataque à distância com projétil mágico (Mages)
}