using System.Collections.Concurrent;
using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Resources;
using Simulation.Core.Ports.ECS;

namespace Simulation.Core.ECS.Sync;

public sealed class WorldInboxSystem(World world, PlayerFactoryResource playerFactoryResource, IWorldSaver saver) : BaseSystem<World, float>(world), IWorldInbox
{
    private readonly ConcurrentQueue<SpawnPlayerRequest> _spawns = new();
    private readonly ConcurrentQueue<DespawnPlayerRequest> _leaves = new();

    public void Enqueue<T>(SpawnPlayerRequest queue) => _spawns.Enqueue(queue);
    public void Enqueue<T>(DespawnPlayerRequest queue) => _leaves.Enqueue(queue);
    
    public override void Update(in float dt)
    {
        while (_spawns.TryDequeue(out var spawn))
        {
            playerFactoryResource.TryCreatePlayer(spawn.Player);
        }

        while (_leaves.TryDequeue(out var leave))
        {
            if (playerFactoryResource.TryDestroyPlayer(leave.PlayerId, out var playerData))
                saver.StageSave(playerData);
        }
    }
    
}