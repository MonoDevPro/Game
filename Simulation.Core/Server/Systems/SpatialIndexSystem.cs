using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Utils.Spatial;

namespace Simulation.Core.Server.Systems;

public sealed partial class SpatialIndexSystem(World world, int mapWidth, int mapHeight) 
    : BaseSystem<World, float>(world)
{
    // Propriedade pública para que outros sistemas possam acessar o índice espacial
    public readonly QuadTreeSpatial SpatialIndex = new(0, 0, mapWidth, mapHeight);
    
    [Query]
    [All<SpatialIndexed>]
    private void RemoveFromIndex(in Entity entity)
    {
        if (!World.IsAlive(entity))
        {
            SpatialIndex.Remove(entity);
        }
    }
    
    [Query]
    [All<Position, LastKnownPosition, SpatialIndexed>]
    private void UpdateIndex(in Entity entity, ref Position currentPos, ref LastKnownPosition lastPos)
    {
        if (currentPos.X != lastPos.X || currentPos.Y != lastPos.Y)
        {
            SpatialIndex.Update(entity, currentPos);
            
            lastPos.X = currentPos.X; // Atualiza a última posição conhecida
            lastPos.Y = currentPos.Y; // Atualiza a última posição conhecida
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
