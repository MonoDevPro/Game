using MemoryPack;

namespace Game.DTOs.Game.Player;

[MemoryPackable]
public readonly partial record struct AttackData(
    int AttackerNetworkId,
    AttackStyle Style,
    float AttackDuration,
    float CooldownRemaining);
    
/// <summary>
/// Define o estilo de ataque baseado na vocação.
/// </summary>
public enum AttackStyle : byte
{
    Melee = 0,   // Ataque corpo a corpo (Warriors)
    Ranged = 1,  // Ataque à distância com projétil físico (Archers)
    Magic = 2    // Ataque à distância com projétil mágico (Mages)
}