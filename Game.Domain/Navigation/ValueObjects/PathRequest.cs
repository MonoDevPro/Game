using Game.Domain.Navigation.Enums;
using Game.Domain.ValueObjects.Map;

namespace Game.Domain.Navigation.ValueObjects;

#region Path Request

/// <summary>
/// Requisição de cálculo de caminho.
/// </summary>
public struct PathRequest
{
    public int TargetX;
    public int TargetY;
    public PathRequestFlags Flags;
    public PathPriority Priority;

    public static PathRequest Create(int targetX, int targetY, 
        PathPriority priority = PathPriority.Normal,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        return new PathRequest
        {
            TargetX = targetX,
            TargetY = targetY,
            Priority = priority,
            Flags = flags
        };
    }

    public static PathRequest Create(GridPosition target,
        PathPriority priority = PathPriority.Normal,
        PathRequestFlags flags = PathRequestFlags.None)
        => Create(target.X, target.Y, priority, flags);
}

#endregion

#region Path State

#endregion

#region Path Buffer

#endregion

#region Tags

#endregion