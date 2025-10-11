namespace Game.Abstractions;

[Flags] public enum InputFlags : ushort 
{ 
    None = 0, 
    ClickLeft = 1 << 0,   // Bit 0
    ClickRight = 1 << 1,  // Bit 1
    Attack = 1 << 2,      // Bit 2
    Sprint = 1 << 3,      // Bit 3
}