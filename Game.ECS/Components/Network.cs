using Game.ECS.Systems;

namespace Game.ECS.Components;

public struct NetworkSync 
{ 
    public uint LastUpdateTick; 
    public bool IsDirty; 
    public SyncFlags Flags; 
}

[Flags]
public enum SyncFlags : byte
{
    None = 0,
    Position = 1 << 0,
    Rotation = 1 << 1,
    Health = 1 << 2,
    Animation = 1 << 3,
    Velocity = 1 << 4,
    Equipment = 1 << 5,
    All = 0xFF
}