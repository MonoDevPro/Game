using Arch.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Infrastructure.ArchECS.Commons;

internal static class ComponentExtensions
{
    public static void RemoveIfExists<T>(this World world, Entity entity) where T : struct
    {
        if (world.Has<T>(entity))
            world.Remove<T>(entity);
    }

    public static void AddOrReplace<T>(this World world, Entity entity, T? component = default) where T : struct
    {
        ref T componentRef = ref world.AddOrGet<T>(entity);

        if (component.HasValue)
            componentRef = component.Value;
    }

}
