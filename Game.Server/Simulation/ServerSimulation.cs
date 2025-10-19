using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Game.Server.Simulation.Systems;

namespace Game.Server.Simulation;

public sealed class ServerSimulation(IServiceProvider provider) : GameSimulation
{
    public override void ConfigureSystems(World world, Group<float> group)
    {
        GameSystem[] serverSystems =
        [
            // 1. Gameplay
            new MovementSystem(world),
            new HealthRegenerationSystem(world),

            // 2. Sincronização de rede
            new PlayerSyncBroadcasterSystem(world, provider.GetRequiredService<INetworkManager>()),
        ];
    }
}