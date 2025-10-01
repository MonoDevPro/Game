using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Builders;
using Simulation.Core.Ports.Network;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Sync;

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
            resources.PlayerNet.RegisterComponentUpdate<Position>(),
            resources.PlayerNet.RegisterComponentUpdate<Direction>(),
            resources.PlayerNet.RegisterComponentUpdate<Health>(),
            resources.PlayerNet.RegisterComponentUpdate<PlayerState>(), // passa a receber estado publicado pelo servidor
        };
        
        return new Group<float>("Net Update Systems", syncSystems);
    }

    protected override ISystem<float> RegisterComponentPost(World world, ClientResourceContext resources)
    {
        var syncOnChangeOption = new SyncOptions(
            SyncFrequency.OneShot, 
            SyncTarget.Server, 
            NetworkDeliveryMethod.ReliableOrdered, 
            NetworkChannel.Simulation, 
            0);
        
        var postSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentPost<MoveIntent>(syncOnChangeOption),
            resources.PlayerNet.RegisterComponentPost<AttackIntent>(syncOnChangeOption),
        };
        return new Group<float>("Net Post Systems", postSystems);
    }

    protected override ISystem<float> CreateSystems(World world, ClientResourceContext resources)
    {
        var systems = new ISystem<float>[]
        {
            new Systems.ClientInputSystem(world, resources.PlayerIndex, new Logger<Systems.ClientInputSystem>(resources.LoggerFactory)),
            new Systems.ClientLogSystem(world, resources.PlayerIndex, new Logger<Systems.ClientLogSystem>(resources.LoggerFactory)),
        };
        
        return new Group<float>("Main Systems", systems);
    }
}