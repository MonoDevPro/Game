namespace Game.ECS.Components;

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

public struct Input
{
    public sbyte InputX; 
    public sbyte InputY; 
    public InputFlags Flags;
    
    public readonly bool HasInput() => InputX != 0 || InputY != 0 || Flags != InputFlags.None;
}