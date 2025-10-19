using Game.ECS.Components;

namespace Game.ECS.Services;

public interface IMapGrid
{
    bool InBounds(int x, int y, int z);
    bool InBounds(in Position p);
    Position ClampToBounds(in Position p);
    bool IsBlocked(in Position p);
    bool AnyBlockedInArea(int minX, int minY, int maxX, int maxY);
    int CountBlockedInArea(int minX, int minY, int maxX, int maxY);
}