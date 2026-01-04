using MemoryPack;

namespace Game.DTOs.Player;

[MemoryPackable]
public readonly partial record struct InputData(
    sbyte InputX, 
    sbyte InputY, 
    InputFlags Flags
);

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
