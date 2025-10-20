namespace Game.ECS.Components;

[Flags] 
public enum InputFlags : ushort 
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
    None      = 0,
    Input     = 1 << 0,
    Movement  = 1 << 1,
    Facing    = 1 << 2,
    Vitals    = 1 << 3,
    All       = Input | Movement | Facing | Vitals
}