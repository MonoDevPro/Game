using Arch.Core;
using Arch.System;
using Simulation.Core.Options;
using Simulation.Core.ECS.Builders;
using Simulation.Core.Ports.Network;
using Simulation.Core.ECS.Components;

namespace Simulation.Core.Client.ECS;

public sealed class ClientSimulationBuilder(IServiceProvider rootProvider) : BaseSimulationBuilder<ClientResourceContext>
{
    protected override ClientResourceContext CreateResourceContext(World world)
    {
        return new ClientResourceContext(rootProvider, world);
    }

    protected override ISystem<float> RegisterComponentUpdate(World world, ClientResourceContext resources)
    {
        var syncSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentUpdate<State>(),
            resources.PlayerNet.RegisterComponentUpdate<Position>(),
            resources.PlayerNet.RegisterComponentUpdate<Direction>(),
            resources.PlayerNet.RegisterComponentUpdate<Health>(),
        };
        
        return new Group<float>("Net Update Systems", syncSystems);
    }

    protected override ISystem<float> RegisterComponentPost(World world, ClientResourceContext resources)
    {
        var postSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentPost<Input>(new SyncOptions(SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, NetworkChannel.Simulation, 0)),
        };
        return new Group<float>("Net Post Systems", postSystems);
    }

    protected override ISystem<float> CreateSystems(World world, ClientResourceContext resources)
    {
        var systems = new ISystem<float>[]
        {
        };
        
        return new Group<float>("Main Systems", systems);
    }
}