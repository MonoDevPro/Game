using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Indexes.Map;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Systems;

 [PipelineSystem(SystemStage.Spatial)]
public sealed partial class SpatialIndexSystem(World world, SpatialOptions options) 
    : BaseSystem<World, float>(world)
{
    // Propriedade pública para que outros sistemas possam acessar o índice espacial
    public readonly QuadTreeSpatial SpatialIndex = new(0, 0, options.Width, options.Height);
    
    [Query]
    [All<Position, LastKnownPosition, SpatialIndexed>]
    private void UpdateIndex(in Entity entity, ref Position currentPos, ref LastKnownPosition lastPos)
    {
        if (currentPos.X != lastPos.X || currentPos.Y != lastPos.Y)
        {
            SpatialIndex.Update(entity, currentPos);
            World.Set<LastKnownPosition>(entity, new LastKnownPosition(currentPos.X, currentPos.Y));
        }
    }

    [Query]
    [All<Position>]
    [None<SpatialIndexed>]
    private void AddToIndex(in Entity entity, ref Position pos)
    {
        SpatialIndex.Add(entity, pos);
        World.Add<SpatialIndexed>(entity);
        World.Add<LastKnownPosition>(entity, new LastKnownPosition { X = pos.X, Y = pos.Y });
    }
    
    [Query]
    [All<Position, SpatialIndexed>]
    [None<LastKnownPosition>]
    private void AddLastKnownPosition(in Entity entity, ref Position pos)
    {
        World.Add<LastKnownPosition>(entity, new LastKnownPosition { X = pos.X, Y = pos.Y });
    }
}
