using Game.ECS.Components;

namespace Game.ECS.Services;

public interface IMapGrid
{
    bool InBounds(SpatialPosition spatialPosition);
    SpatialPosition ClampToBounds(SpatialPosition spatialPosition);
    bool IsBlocked(SpatialPosition spatialPosition);
    bool AnyBlockedInArea(SpatialPosition min, SpatialPosition max);
    int CountBlockedInArea(SpatialPosition min, SpatialPosition max);
}