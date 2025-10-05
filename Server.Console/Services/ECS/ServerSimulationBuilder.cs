using Arch.Core;
using Arch.System;
using Server.Console.Services.ECS.Systems;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Sync;
using Simulation.Core.ECS.Utils;
using Simulation.Core.Ports.Network;

namespace Server.Console.Services.ECS;

public sealed class ServerSimulationBuilder(IServiceProvider rootProvider) : BaseSimulationBuilder<ServerResourceContext>
{
    protected override ServerResourceContext CreateResourceContext(World world, MapService? mapService)
    {
        return new ServerResourceContext(rootProvider, world, mapService ?? throw new ArgumentNullException(nameof(mapService)));
    }

    protected override ISystem<float> RegisterComponentUpdate(World world, ServerResourceContext resources)
    {
        var syncSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentUpdate<MoveIntent>(),
            resources.PlayerNet.RegisterComponentUpdate<AttackIntent>(),
        };
        return new Group<float>("Net Update Systems", syncSystems);
    }

    protected override Group<float> RegisterComponentPost(World world, ServerResourceContext resources)
    {
        var syncOnChangeOption = new SyncOptions(
            SyncFrequency.OnChange, 
            SyncTarget.Broadcast, 
            NetworkDeliveryMethod.ReliableOrdered, 
            NetworkChannel.Simulation, 
            0);
        
        var postSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentPost<PlayerState>(syncOnChangeOption),
            resources.PlayerNet.RegisterComponentPost<Direction>(syncOnChangeOption),
            resources.PlayerNet.RegisterComponentPost<Position>(syncOnChangeOption),
            resources.PlayerNet.RegisterComponentPost<Health>(syncOnChangeOption),
        };
        return new Group<float>("Net Post Systems", postSystems);
    }

    protected override ISystem<float> CreateSystems(World world, ServerResourceContext resources)
    {
        var systems = new ISystem<float>[]
        {
            new DevTestSpawnSystem(world, resources.PlayerFactory, resources.GetLogger<DevTestSpawnSystem>()), // Spawna 1 player de teste
            new WorldInboxSystem(world, resources.PlayerFactory, resources.PlayerSave),
            new MoveSystem(world, resources.SpatialIndex),
            new AttackSystem(world),
            new HealthTickSystem(world),
        };
        
        return new Group<float>("Main Systems", systems);
    }
}