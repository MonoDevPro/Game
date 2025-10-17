namespace Game.ECS.Components;

[Flags] public enum InputFlags : ushort 
{ 
    None = 0, 
    ClickLeft = 1 << 0,   // Bit 0
    ClickRight = 1 << 1,  // Bit 1
    Attack = 1 << 2,      // Bit 2
    Sprint = 1 << 3,      // Bit 3
}

[Flags]
public enum SyncFlags : ulong
{
    None = 0,
    Position = 1UL << 0,
    Facing = 1UL << 1,
    Velocity = 1UL << 2,
    Health = 1UL << 3,
    Mana = 1UL << 4,
    
    // ========== AÇÕES AGRUPADAS (32-47) ==========
    Movement       = Position | Velocity | Facing,
    Vitals         = Health | Mana,
    
    // ✅ Apenas as flags que você REALMENTE usa
    InitialSync = Movement | Vitals,
}