using Game.Domain.Navigation.Enums;

namespace Game.Domain.Navigation.ValueObjects;

/// <summary>
/// Estado do pathfinding de uma entidade.
/// </summary>
public struct PathState
{
    public PathStatus Status;
    public PathFailReason FailReason;
    public byte AttemptCount;
    public long LastUpdateTick;

    public readonly bool IsActive => Status is PathStatus.Pending 
        or PathStatus.Computing or PathStatus.Ready or PathStatus.Following;

    public readonly bool HasFailed => Status == PathStatus.Failed;
    public readonly bool IsComplete => Status == PathStatus.Completed;
}