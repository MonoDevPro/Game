using System.Runtime.InteropServices;
using Game.Domain.VOs;

namespace Game.ECS.Components;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerInput
{
    public uint SequenceNumber;
    public Coordinate Movement; // -1 a 1
    public Coordinate Look;     // -1 a 1
    public InputFlags Flags;     // Botões pressionados
}

[Flags] public enum InputFlags : ushort 
{ 
    None = 0, 
    Attack = 1 << 0, 
    Interact = 1 << 1, 
    Sprint = 1 << 2
}