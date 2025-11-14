using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Extensions;

public static class DirtyExtensions
{
    public static void MarkDirty(this World world, Entity entity, 
        DirtyComponentType componentType)
    {
        if (!world.IsAlive(entity)) return;
        if (!world.Has<DirtyFlags>(entity)) return;
        ref var dirtyFlags = ref world.Get<DirtyFlags>(entity);
        dirtyFlags.MarkDirty(componentType);
    }
}