using Arch.Core;
using Arch.Relationships;
using Simulation.Core.ECS.Server.Systems.Indexes;
using Simulation.Core.ECS.Shared;

namespace Simulation.Core.ECS.Server.Systems.Relationships;

public static class PlayerRelationship
{
    public static List<int> GetPlayerIdsInMap(this World world, MapInstance map, List<int> reuseList)
    {
        ref var relationships = ref world.GetRelationships<PlayerId>(map.Entity);
        var enumerator = relationships.GetEnumerator();
        while (enumerator.MoveNext())
            reuseList.Add(enumerator.Current.Value.Value);
        return reuseList;
    }

    public static List<Entity> GetMapPlayerEntities(this World world, MapInstance map, List<Entity> reuseList)
    {
        ref var relationships = ref world.GetRelationships<PlayerId>(map.Entity);
        var enumerator = relationships.GetEnumerator();
        while (enumerator.MoveNext())
            reuseList.Add(enumerator.Current.Key);
        return reuseList;
    }
    
    public static void AddPlayerToMap<T>(this World world, Entity playerEntity, MapInstance map, T identifier) where T : struct
    {
        world.AddRelationship<T>(map.Entity, playerEntity, identifier);
    }
}