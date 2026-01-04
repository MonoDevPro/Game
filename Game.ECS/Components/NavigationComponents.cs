using Game.ECS.Navigation.Components;

namespace Game.ECS.Components;

[Flags] public enum PathRequestFlags : byte { None = 0, AllowPartial = 1 << 0, CardinalOnly = 1 << 1, HighPriority = 1 << 2 }

public enum PathStatus : byte { None = 0, Pending, Ready, Following, Completed, Failed }

public partial struct AgentConfig 
{ 
    public int MoveTickDuration; 
    public int DiagonalTickDuration; 
    public bool AllowDiagonal; 
    public int MaxPathLength; 
}

/// <summary>
/// Buffer de caminho do servidor. 
/// </summary>
public partial struct PathBuffer { 
    public const int MaxWaypoints = 64; 
    
    public unsafe fixed short WaypointsX[MaxWaypoints]; 
    public unsafe fixed short WaypointsY[MaxWaypoints]; 
    public unsafe fixed short WaypointsZ[MaxWaypoints];
    public byte WaypointCount; 
    public byte CurrentIndex; 
    public short GoalX; 
    public short GoalY;
    public short GoalZ;
}

/// <summary>
/// Estado do pathfinding. 
/// </summary>
public struct PathState { public PathStatus Status; public PathFailReason FailReason; public byte RetryCount; }

/// <summary>
/// Request de pathfinding.
/// </summary>
public struct PathRequest { public short TargetX; public short TargetY; public PathRequestFlags Flags; }

/// <summary>
/// Tag:  entidade é um agente de navegação.
/// </summary>
public struct EntityNavigationAgent { }

/// <summary>
/// Tag: entidade é obstáculo dinâmico. 
/// </summary>
public struct Obstacle { public byte Radius; }