using Arch.Core;
using Arch.System;
using Game. ECS.Navigation. Components;
using Game.ECS.Navigation.Data;

namespace Game. ECS.Navigation. Systems;

/// <summary>
/// Processa requisições de pathfinding no servidor.
/// </summary>
public sealed class ServerPathRequestSystem : BaseSystem<World, long>
{
    private readonly GridPathfindingSystem _pathfinder;
    private readonly int _maxPerTick;
    private int _processedThisTick;

    public ServerPathRequestSystem(
        World world,
        GridPathfindingSystem pathfinder,
        int maxPerTick = 50) : base(world)
    {
        _pathfinder = pathfinder;
        _maxPerTick = maxPerTick;
    }

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
            if (_processedThisTick >= _maxPerTick)
                return;

            if (state.Status != PathStatus. Pending)
                return;

            _processedThisTick++;
            state.Status = PathStatus.Computing;

            var result = _pathfinder.FindPath(
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