using Arch.Core;
using Arch.System;
using Game. ECS.Navigation. Components;
using Game.ECS.Navigation.Data;

namespace Game. ECS.Navigation. Systems;

/// <summary>
/// Processa requisições de pathfinding no servidor.
/// </summary>
public sealed class ServerPathRequestSystem(
    World world,
    GridPathfindingSystem pathfinder,
    int maxPerTick = 50)
    : BaseSystem<World, long>(world)
{
    private int _processedThisTick;

    public override void BeforeUpdate(in long serverTick)
    {
        _processedThisTick = 0;
    }

    public override void Update(in long serverTick)
    {
        var query = new QueryDescription()
            .WithAll<PathRequest, GridPosition, PathState, GridPathBuffer, GridNavigationAgent>();

        World.Query(in query, (Entity entity,
            ref PathRequest request,
            ref GridPosition pos,
            ref PathState state,
            ref GridPathBuffer buffer) =>
        {
            if (_processedThisTick >= maxPerTick)
                return;

            if (state.Status != PathStatus. Pending)
                return;

            _processedThisTick++;
            state.Status = PathStatus.Computing;

            var result = pathfinder.FindPath(
                pos.X, pos. Y,
                request.TargetX, request.TargetY,
                ref buffer,
                request. Flags);

            if (result.Success)
            {
                state.Status = PathStatus.Ready;
                state.FailReason = PathFailReason.None;
            }
            else
            {
                state.Status = PathStatus.Failed;
                state.FailReason = (PathFailReason)(byte)result.FailReason;
                state.AttemptCount++;
            }

            World.Remove<PathRequest>(entity);
        });
    }
}