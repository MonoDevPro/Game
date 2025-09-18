using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.ECS.Pipeline;

namespace Simulation.Core.ECS.Systems;

 [PipelineSystem(SystemStage.Spatial)]
public sealed partial class SpatialIndexSystem(World world, WorldSpatial spatial, MapService map) : BaseSystem<World, float>(world)
{
    [Query]
    [All<SpatialUnindexed, SpatialIndexed>]
    private void RemoveFromIndex(in Entity entity)
    {
        World.Remove<SpatialUnindexed>(entity);
        spatial.Remove(entity);
    }
    
    [Query]
    [All<Position, LastKnownPosition, SpatialIndexed>]
    private void UpdateIndex(in Entity entity, ref Position currentPos, ref LastKnownPosition lastPos)
    {
        if (!currentPos.Equals(lastPos.Position))
        {
            if (!spatial.Move(entity, currentPos) || map.IsBlocked(currentPos))
            {
                // Reverte para a última posição válida se a nova for inválida
                currentPos = lastPos.Position;
            }
            
            World.Set<LastKnownPosition>(entity, new LastKnownPosition(currentPos));
        }
    }

    [Query]
    [All<Position>]
    [None<SpatialIndexed>]
    private void AddToIndex(in Entity entity, ref Position pos)
    {
        spatial.Add(entity, pos);
        World.Add<LastKnownPosition>(entity, new LastKnownPosition(pos));

        World.Add<SpatialIndexed>(entity);
    }
    
    [Query]
    [All<Position, SpatialIndexed>]
    [None<LastKnownPosition>]
    private void AddLastKnownPosition(in Entity entity, ref Position pos)
    {
        World.Add<LastKnownPosition>(entity, new LastKnownPosition(pos));
    }
}
