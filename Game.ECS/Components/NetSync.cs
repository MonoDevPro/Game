using System.Runtime.InteropServices;

namespace Game.ECS.Components;

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

// Network
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NetworkDirty
{
    public SyncFlags Flags;
}