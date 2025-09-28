using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Components;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;
using Simulation.Core.Server.ECS.Systems;

namespace Simulation.Core.Server.ECS;

public sealed class ServerSimulationBuilder(IServiceProvider rootProvider) : BaseSimulationBuilder<ServerResourceContext>
{
    protected override ServerResourceContext CreateResourceContext(World world)
    {
        return new ServerResourceContext(rootProvider, world);
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

    protected override ISystem<float> RegisterComponentPost(World world, ServerResourceContext resources)
    {
        var syncOnChangeOption = new SyncOptions(
            SyncFrequency.OneShot, 
            SyncTarget.Server, 
            NetworkDeliveryMethod.ReliableOrdered, 
            NetworkChannel.Simulation, 
            0);
        
        var postSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentPost<PlayerState>(syncOnChangeOption),
            resources.PlayerNet.RegisterComponentPost<Position>(syncOnChangeOption),
            resources.PlayerNet.RegisterComponentPost<Direction>(syncOnChangeOption),
            resources.PlayerNet.RegisterComponentPost<Health>(syncOnChangeOption),
        };
        return new Group<float>("Net Post Systems", postSystems);
    }

    protected override ISystem<float> CreateSystems(World world, ServerResourceContext resources)
    {
        var systems = new ISystem<float>[]
        {
            new WorldInboxSystem(world, resources.PlayerFactory, resources.PlayerSave),
            new MoveSystem(world),
            new AttackSystem(world),
        };
        
        return new Group<float>("Main Systems", systems);
    }
}