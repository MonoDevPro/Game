using MemoryPack;

namespace Game.ECS.Services.Snapshot.Data;

[MemoryPackable]
public readonly partial record struct PlayerInputRequest(
    sbyte InputX, 
    sbyte InputY, 
    InputFlags Flags
);

[MemoryPackable]
public readonly partial record struct PlayerInputPacket(PlayerInputRequest Input);

// ============================================
// Inputs - Entrada do jogador
// ============================================
[Flags] 
public enum InputFlags : ushort 
{ 
    None = 0, 
    ClickLeft = 1 << 0,   // Clique esquerdo
    ClickRight = 1 << 1,  // Clique direito
    BasicAttack = 1 << 2, // Atacar (hotkey)
    Sprint = 1 << 3,      // Correr
}
