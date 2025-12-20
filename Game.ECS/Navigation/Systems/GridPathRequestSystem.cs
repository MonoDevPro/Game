using Arch.Core;
using Arch.System;
using Game.ECS. Navigation.Components;
using Game.ECS.Navigation.Data;
using PathFailReason = Game.ECS.Navigation.Components.PathFailReason;

namespace Game. ECS.Navigation. Systems;

/// <summary>
/// Processa requisições de pathfinding no grid.
/// </summary>
public sealed class GridPathRequestSystem(
    World world,
    GridPathfindingSystem pathfinder,
    NavigationConfig? config = null)
    : BaseSystem<World, float>(world)
{
    private readonly NavigationConfig _config = config ??  NavigationConfig.Default;
    private int _requestsThisTick;

    public override void BeforeUpdate(in float deltaTime)
    {
        _requestsThisTick = 0;
    }

    public override void Update(in float deltaTime)
    {
        ProcessRequests(World);
    }

    private void ProcessRequests(World world)
    {
        var query = new QueryDescription()
            .WithAll<PathRequest, GridPosition, PathState, GridPathBuffer, GridNavigationAgent>();

        world.Query(in query, (Entity entity,
            ref PathRequest request,
            ref GridPosition pos,
            ref PathState state,
            ref GridPathBuffer buffer) =>
        {
            // Limita requisições por tick
            if (_requestsThisTick >= _config.MaxRequestsPerTick)
                return;

            if (state.Status != PathStatus. Pending)
                return;

            _requestsThisTick++;
            state.Status = PathStatus.Computing;

            // Calcula caminho
            var result = pathfinder.FindPath(
                pos.X, pos. Y,
                request. TargetX, request.TargetY,
                ref buffer,
                request. Flags);

            if (result.Success)
            {
                state.Status = PathStatus.Ready;
                state. TimeCompleted = Environment.TickCount64 / 1000f;
                state. FailReason = PathFailReason.None;
            }
            else
            {
                state.Status = PathStatus.Failed;
                state. FailReason = (PathFailReason)(byte)result.FailReason;
                state.AttemptCount++;
            }

            // Remove request após processar
            world.Remove<PathRequest>(entity);
        });
    }
}