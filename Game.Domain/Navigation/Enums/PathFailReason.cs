namespace Game.Domain.Navigation.Enums;

public enum PathFailReason : byte
{
    None = 0,
    NoPathExists,
    StartBlocked,
    GoalBlocked,
    Timeout,
    TooFarAway,
    InvalidRequest,
    AlreadyAtGoal,
    BufferTooSmall
}