namespace Game.ECS.Services.Navigation.Components;

public enum PathRequestFlags : byte
{
    None = 0,
    AllowPartialPath = 1 << 0,
    HighPriority = 1 << 1,
    IgnoreEntities = 1 << 2
}

public enum PathPriority : byte
{
    Low = 0,
    Normal = 1,
    High = 2,
    Critical = 3
}

public enum PathStatus : byte
{
    None = 0,
    Pending = 1,
    InProgress = 2,
    Completed = 3,
    Failed = 4,
    Cancelled = 5
}

public enum PathFailReason : byte
{
    None = 0,
    NoPath = 1,
    Timeout = 2,
    InvalidStart = 3,
    InvalidGoal = 4,
    Cancelled = 5
}