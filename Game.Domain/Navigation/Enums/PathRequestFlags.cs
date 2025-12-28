namespace Game.Domain.Navigation.Enums;

[Flags]
public enum PathRequestFlags : byte
{
    None = 0,
    AllowPartialPath = 1 << 0,
    IgnoreDynamicObstacles = 1 << 1,
    CardinalOnly = 1 << 2,
    Revalidate = 1 << 3
}