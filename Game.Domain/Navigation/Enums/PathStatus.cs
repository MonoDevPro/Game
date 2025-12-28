namespace Game.Domain.Navigation.Enums;

public enum PathStatus : byte
{
    None = 0,
    Pending,
    Computing,
    Ready,
    Following,
    Completed,
    Failed,
    Cancelled
}