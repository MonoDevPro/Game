using System.Runtime.CompilerServices;

namespace Game.ECS.Components;

/// <summary>
/// Configurações de movimento do agente.
/// </summary>
public partial struct AgentConfig 
{
    public static AgentConfig Default => new()
    {
        MoveTickDuration = 6,        // 100ms a 60 ticks/s
        DiagonalTickDuration = 9,    // ~141ms (sqrt(2) * 100ms)
        AllowDiagonal = true,
        MaxPathLength = 64
    };

    public static AgentConfig Slow => new()
    {
        MoveTickDuration = 12,
        DiagonalTickDuration = 17,
        AllowDiagonal = true,
        MaxPathLength = 64
    };

    public static AgentConfig Fast => new()
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
public partial struct PathBuffer
{
    public readonly bool IsValid => WaypointCount > 0;
    public readonly bool IsComplete => CurrentIndex >= WaypointCount;
    public readonly int Remaining => WaypointCount - CurrentIndex;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe void SetWaypoint(int index, int x, int y, int z)
    {
        if (index is < 0 or >= MaxWaypoints) return;
        WaypointsX[index] = (short)x;
        WaypointsY[index] = (short)y;
        WaypointsZ[index] = (short)z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public unsafe Position GetWaypoint(int index)
    {
        if (index >= 0 && index < WaypointCount)
        {
            return new Position
            {
                X = WaypointsX[index], 
                Y = WaypointsY[index], 
                Z = WaypointsZ[index]
            };
        }
        return new Position { X = -1, Y = -1, Z = -1 };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Position GetCurrentWaypoint() 
        => GetWaypoint(CurrentIndex);

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public bool AdvanceWaypoint()
    {
        if (CurrentIndex >= WaypointCount) return false;
        CurrentIndex++;
        return true;
    }

    public void Clear()
    {
        WaypointCount = 0;
        CurrentIndex = 0;
    }
}