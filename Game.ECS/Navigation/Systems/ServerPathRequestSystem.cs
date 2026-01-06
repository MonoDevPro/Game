using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game. ECS.Navigation. Components;
using Game.ECS.Services.Pathfinding;
using Game.ECS.Services.Pathfinding.Systems;

namespace Game. ECS.Navigation. Systems;

/// <summary>
/// Processa requisições de pathfinding no servidor.
/// </summary>
public sealed partial class ServerPathRequestSystem(
    World world,
    PathfindingSystem pathfinder,
    int maxPerTick = 50)
    : BaseSystem<World, long>(world)
{
    private int _processedThisTick;

    public override void BeforeUpdate(in long serverTick)
    {
        _processedThisTick = 0;
    }
    
    [Query]
    [All<PathfindingRequest, GridPosition, PathBuffer, GridNavigationAgent>]
    private void ProcessPathRequests(
        [Data]in long tick,
        in Entity entity)
    {
    }
}