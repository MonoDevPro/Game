namespace Game.ECS.Components;

public struct PlayerInput
{
    public Coordinate Movement; 
    public Coordinate Look; 
    public InputFlags Flags; 
    public uint SequenceNumber;
}

[Flags] public enum InputFlags : ushort 
{ 
    None = 0, 
    Attack = 1 << 0, 
    Interact = 1 << 1, 
    Sprint = 1 << 5 
}