namespace Game.ECS.Navigation.Components;

/// <summary>
/// Requisição de pathfinding pendente.
/// </summary>
public struct NavPathRequest
{
    public int TargetX;
    public int TargetY;
    public int TargetZ;
    public PathRequestFlags Flags;
    public PathPriority Priority;
}