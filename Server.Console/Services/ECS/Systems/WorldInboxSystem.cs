using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using GameWeb.Application.Players.Models;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Resource;
using Simulation.Core.Ports.ECS;

namespace Server.Console.Services.ECS.Systems;

public sealed class WorldInboxSystem(World world, PlayerFactoryResource playerFactoryResource, PlayerSaveResource playerSaveResource) : BaseSystem<World, float>(world), IWorldInbox
{
    private readonly ConcurrentQueue<SpawnPlayerRequest> _spawns = new();
    private readonly ConcurrentQueue<DespawnPlayerRequest> _leaves = new();
    
    public void Enqueue<T>(SpawnPlayerRequest queue) => _spawns.Enqueue(queue);
    public void Enqueue<T>(DespawnPlayerRequest queue) => _leaves.Enqueue(queue);
    
    public override void Update(in float dt)
    {
        while (_spawns.TryDequeue(out var spawn))
            playerFactoryResource.TryCreatePlayer(spawn.Player, out _);

        while (_leaves.TryDequeue(out var leave))
        {
            if (playerFactoryResource.TryDestroyPlayer(leave.PlayerId, out PlayerDto? data) && data is not null)
                playerSaveResource.SavePlayer(data);
        }
    }
}