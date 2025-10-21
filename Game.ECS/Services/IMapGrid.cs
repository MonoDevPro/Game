using Game.ECS.Components;

namespace Game.ECS.Services;

public interface IMapGrid
{
    bool InBounds(Position p);
    Position ClampToBounds(Position p);
    bool IsBlocked(Position p);
    bool AnyBlockedInArea(Position min, Position max);
    int CountBlockedInArea(Position min, Position max);
}