namespace Game.Infrastructure.ArchECS.Services.Navigation.Components;

/// <summary>
/// Requisição de pathfinding pendente.
/// </summary>
public struct NavPathRequest
{
    public int TargetX;
    public int TargetY;
    public int TargetFloor;
    public PathRequestFlags Flags;
    public PathPriority Priority;
}