using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Navigation.Shared.Components;
using Game.ECS.Navigation.Shared.Systems;

namespace Game.ECS.Navigation.Server.Systems;

/// <summary>
/// Processa requisições de pathfinding no servidor.
/// </summary>
public sealed partial class ServerPathRequestSystem(World world, PathfindingService pathfinder, int maxPerTick = 50)
    : BaseSystem<World, long>(world)
{
    private int _processedThisTick;

    public override void BeforeUpdate(in long tick) => _processedThisTick = 0;

    [Query]
    [All<PathRequest, GridPosition, PathState, GridPathBuffer, NavigationAgent>]
    private void ProcessRequests([Data] in long tick, in Entity entity, ref PathRequest request, ref GridPosition pos,
        ref PathState state, ref GridPathBuffer buffer)
    {
        if (_processedThisTick >= maxPerTick) return;
        if (state.Status != PathStatus.Pending) return;

        _processedThisTick++;
        state.Status = PathStatus.Computing;
        state.LastUpdateTick = tick;

        var result = pathfinder.FindPath(pos, new GridPosition(request.TargetX, request.TargetY), ref buffer, request.Flags);

        if (result.Success)
        {
            state.Status = PathStatus.Ready;
            state.FailReason = PathFailReason.None;
        }
        else
        {
            state.Status = PathStatus.Failed;
            state.FailReason = result.FailReason;
            state.AttemptCount++;
        }

        World.Remove<PathRequest>(entity);
    }
}