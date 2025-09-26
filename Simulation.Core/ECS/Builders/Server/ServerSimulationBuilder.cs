using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Builders.Commons;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Builders.Server;

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
            resources.PlayerNet.RegisterComponentUpdate<Input>(),
            resources.PlayerNet.RegisterComponentUpdate<Direction>(),
            resources.PlayerNet.RegisterComponentUpdate<DespawnPlayerRequest>(),
        };
        return new Group<float>("Net Update Systems", syncSystems);
    }

    protected override ISystem<float> RegisterComponentPost(World world, ServerResourceContext resources)
    {
        var postSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentPost<State>(new SyncOptions(Authority.Server, SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, 0)),
            resources.PlayerNet.RegisterComponentPost<Position>(new SyncOptions(Authority.Server, SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, 0)),
            resources.PlayerNet.RegisterComponentPost<Direction>(new SyncOptions(Authority.Server, SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, 0)),
            resources.PlayerNet.RegisterComponentPost<Health>(new SyncOptions(Authority.Server, SyncFrequency.OnChange, SyncTarget.Broadcast, NetworkDeliveryMethod.ReliableOrdered, 0)),
        };
        return new Group<float>("Net Post Systems", postSystems);
    }

    protected override ISystem<float> CreateSystems(World world, ServerResourceContext resources)
    {
        var systems = new ISystem<float>[]
        {
            new MovementSystem(world),
        };
        
        return new Group<float>("Main Systems", systems);
    }
}