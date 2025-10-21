namespace Game.ECS.Components;

[Flags] 
public enum InputFlags : ushort 
{ 
    None = 0, 
    ClickLeft = 1 << 0,   // Clique esquerdo
    ClickRight = 1 << 1,  // Clique direito
    Attack = 1 << 2,      // Atacar (hotkey)
    Sprint = 1 << 3,      // Correr
    Ability1 = 1 << 4,    // Habilidade 1
    Ability2 = 1 << 5,    // Habilidade 2
    Ability3 = 1 << 6,    // Habilidade 3
    Ability4 = 1 << 7,    // Habilidade 4
    UseItem = 1 << 8,     // Usar item
    Interact = 1 << 9,    // Interagir com NPC/Objeto
}
