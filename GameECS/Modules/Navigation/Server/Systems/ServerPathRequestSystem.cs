using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using GameECS.Modules.Navigation.Shared.Components;
using GameECS.Modules.Navigation.Shared.Systems;

namespace GameECS.Modules.Navigation.Server.Systems;

/// <summary>
/// Processa requisições de pathfinding no servidor.
/// </summary>
public sealed partial class ServerPathRequestSystem : BaseSystem<World, long>
{
    private readonly PathfindingService _pathfinder;
    private readonly int _maxPerTick;
    private int _processedThisTick;
    private long _currentTick;

    public ServerPathRequestSystem(World world, PathfindingService pathfinder, int maxPerTick = 50) 
        : base(world)
    {
        _pathfinder = pathfinder;
        _maxPerTick = maxPerTick;
    }

    public override void BeforeUpdate(in long tick) => _processedThisTick = 0;

    public override void Update(in long tick)
    {
        _currentTick = tick;
        ProcessPathRequestsQuery(World);
    }

    [Query]
    [All<PathRequest, GridPosition, PathState, GridPathBuffer, NavigationAgent>]
    private void ProcessPathRequests(
        Entity entity,
        ref PathRequest request,
        ref GridPosition pos,
        ref PathState state,
        ref GridPathBuffer buffer)
    {
        if (_processedThisTick >= _maxPerTick) return;
        if (state.Status != PathStatus.Pending) return;

        _processedThisTick++;
        state.Status = PathStatus.Computing;
        state.LastUpdateTick = _currentTick;

        var result = _pathfinder.FindPath(pos, new GridPosition(request.TargetX, request.TargetY), ref buffer, request.Flags);

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