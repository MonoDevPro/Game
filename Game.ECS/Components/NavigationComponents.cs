using System.Runtime.CompilerServices;
using Game.ECS.Navigation.Components;

namespace Game.ECS.Components;

/// <summary>
/// Posição autoritativa no grid (SERVIDOR).
/// Esta é a única fonte de verdade. 
/// </summary>
public struct ServerGridPosition(int x, int y) : IEquatable<ServerGridPosition>
{
    public int X = x;
    public int Y = y;

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public readonly bool Equals(ServerGridPosition other) 
        => X == other.X && Y == other.Y;

    public readonly override int GetHashCode() 
        => HashCode.Combine(X, Y);

    public static bool operator ==(ServerGridPosition left, ServerGridPosition right) 
        => left. Equals(right);

    public static bool operator !=(ServerGridPosition left, ServerGridPosition right) 
        => !left. Equals(right);

    public readonly override bool Equals(object?  obj) 
        => obj is ServerGridPosition other && Equals(other);
}


/// <summary>
/// Configurações de movimento do agente.
/// </summary>
public struct ServerAgentConfig
{
    public int MoveTickDuration;      // Ticks para mover 1 célula
    public int DiagonalTickDuration;  // Ticks para diagonal (maior)
    public bool AllowDiagonal;
    public int MaxPathLength;

    public static ServerAgentConfig Default => new()
    {
        MoveTickDuration = 6,        // 100ms a 60 ticks/s
        DiagonalTickDuration = 9,    // ~141ms (sqrt(2) * 100ms)
        AllowDiagonal = true,
        MaxPathLength = 64
    };

    public static ServerAgentConfig Slow => new()
    {
        MoveTickDuration = 12,
        DiagonalTickDuration = 17,
        AllowDiagonal = true,
        MaxPathLength = 64
    };

    public static ServerAgentConfig Fast => new()
    {
        MoveTickDuration = 3,
        DiagonalTickDuration = 4,
        AllowDiagonal = true,
        MaxPathLength = 64
    };
}

/// <summary>
/// Buffer de caminho do servidor. 
/// </summary>
public struct ServerPathBuffer
{
    public const int MaxWaypoints = 64;
    
    public unsafe fixed short WaypointsX[MaxWaypoints];
    public unsafe fixed short WaypointsY[MaxWaypoints];
    
    public byte WaypointCount;
    public byte CurrentIndex;
    public short GoalX;
    public short GoalY;

    public readonly bool IsValid => WaypointCount > 0;
    public readonly bool IsComplete => CurrentIndex >= WaypointCount;
    public readonly int Remaining => WaypointCount - CurrentIndex;

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public unsafe void SetWaypoint(int index, int x, int y)
    {
        if (index >= 0 && index < MaxWaypoints)
        {
            WaypointsX[index] = (short)x;
            WaypointsY[index] = (short)y;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe ServerGridPosition GetWaypoint(int index)
    {
        if (index >= 0 && index < WaypointCount)
        {
            return new ServerGridPosition(WaypointsX[index], WaypointsY[index]);
        }
        return new ServerGridPosition(-1, -1);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ServerGridPosition GetCurrentWaypoint() 
        => GetWaypoint(CurrentIndex);

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public bool AdvanceWaypoint()
    {
        if (CurrentIndex < WaypointCount)
        {
            CurrentIndex++;
            return true;
        }
        return false;
    }

    public void Clear()
    {
        WaypointCount = 0;
        CurrentIndex = 0;
    }
}

/// <summary>
/// Estado do pathfinding. 
/// </summary>
public struct ServerPathState
{
    public PathStatus Status;
    public PathFailReason FailReason;
    public byte RetryCount;
}

public enum PathStatus : byte
{
    None = 0,
    Pending,
    Ready,
    Following,
    Completed,
    Failed
}

/// <summary>
/// Request de pathfinding.
/// </summary>
public struct ServerPathRequest
{
    public short TargetX;
    public short TargetY;
    public PathRequestFlags Flags;
}

[Flags]
public enum PathRequestFlags :  byte
{
    None = 0,
    AllowPartial = 1 << 0,
    CardinalOnly = 1 << 1,
    HighPriority = 1 << 2
}

/// <summary>
/// Tag:  entidade é um agente de navegação.
/// </summary>
public struct ServerNavigationAgent { }

/// <summary>
/// Tag: entidade é obstáculo dinâmico. 
/// </summary>
public struct ServerObstacle 
{
    public byte Radius;
}