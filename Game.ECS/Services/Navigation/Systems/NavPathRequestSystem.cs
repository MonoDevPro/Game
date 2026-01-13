using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Navigation.Components;
using Game.ECS.Services.Map;
using Game.ECS.Services.Navigation.Components;

namespace Game.ECS.Services.Navigation.Systems;

/// <summary>
/// Processa requisições de pathfinding no servidor.
/// </summary>
public sealed partial class NavPathRequestSystem(
    World world,
    PathfindingSystem pathfinder,
    WorldMap grid,
    int maxPerTick = 50)
    : BaseSystem<World, long>(world)
{
    private int _processedThisTick;

    public override void BeforeUpdate(in long serverTick)
    {
        _processedThisTick = 0;
    }

    [Query]
    [All<NavPathRequest, Position, NavPathBuffer, NavPathState, NavAgent>]
    private void ProcessPathRequests(
        [Data] in long tick,
        in Entity entity,
        ref NavPathRequest request,
        ref Position pos,
        ref NavPathBuffer buffer,
        ref NavPathState state)
    {
        if (_processedThisTick >= maxPerTick)
            return;

        _processedThisTick++;

        // Cria request de pathfinding
        var pathRequest = new PathfindingRequest
        {
            StartX = pos.X,
            StartY = pos.Y,
            StartZ = pos.Z,
            GoalX = request.TargetX,
            GoalY = request.TargetY,
            GoalZ = request.TargetZ,
            MaxSearchNodes = 2048,
            Status = PathfindingStatus.Pending
        };

        // Executa pathfinding
        Span<int> pathBuffer = stackalloc int[NavPathBuffer.MaxWaypoints];
        var result = pathfinder.FindPath(ref pathRequest, pathBuffer);

        // Processa resultado
        if (result.IsValid && result.PathLength > 0)
        {
            // Copia waypoints para o buffer
            buffer.Clear();
            buffer.GoalX = request.TargetX;
            buffer.GoalY = request.TargetY;
            buffer.GoalZ = request.TargetZ;

            for (int i = 0; i < result.PathLength && i < NavPathBuffer.MaxWaypoints; i++)
            {
                buffer.SetWaypoint(i, pathBuffer[i]);
            }
            buffer.WaypointCount = Math.Min(result.PathLength, NavPathBuffer.MaxWaypoints);

            state.Status = PathStatus.InProgress;
            state.FailReason = PathFailReason.None;
        }
        else
        {
            state.Status = PathStatus.Failed;
            state.FailReason = PathFailReason.NoPath;
        }

        // Remove a request processada
        World.Remove<NavPathRequest>(entity);
    }
}
