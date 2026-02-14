using Arch.Core;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Game.Infrastructure.ArchECS.Commons;

internal static class NetworkExtensions
{
    public static int ResolveNetworkId(this World world, Entity entity)
    {
        if (entity != Entity.Null && world.Has<CharacterId>(entity))
        {
            return world.Get<CharacterId>(entity).Value;
        }

        return entity.Id;
    }
}
