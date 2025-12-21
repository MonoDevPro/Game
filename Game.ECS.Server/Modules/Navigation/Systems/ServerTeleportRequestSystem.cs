using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Server.Modules.Navigation.Components;
using Game.ECS.Shared.Components.Navigation;

namespace Game.ECS.Server.Modules.Navigation.Systems;

public sealed partial class ServerTeleportRequestSystem(World word, int maxPerTick = 50)
    : BaseSystem<World, long>(word)
{
    private int _processedThisTick;

    public override void BeforeUpdate(in long tick) => _processedThisTick = 0;
    
    [Query]
    [All<TeleportRequest, GridPosition>]
    private void ProcessTeleportRequests(
        in Entity entity,
        in TeleportRequest spawnRequest,
        ref GridPosition position)
    {
        if (_processedThisTick >= maxPerTick) return;
        _processedThisTick++;

        position.X = spawnRequest.Position.X;
        position.Y = spawnRequest.Position.Y;
        
        World.Remove<TeleportRequest>(entity);
    }
}