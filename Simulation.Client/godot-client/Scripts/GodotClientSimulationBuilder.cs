using System;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Simulation.Core.Client.ECS;
using Simulation.Core.ECS.Builders;
using Simulation.Core.ECS.Components;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;

namespace GodotClient;

/// <summary>
/// Simulation builder específico para o cliente Godot:
/// - Recebe Position/Direction/Health/PlayerState do servidor
/// - Publica MoveIntent/AttackIntent para o servidor (OneShot)
/// - Mantém um DevTestSpawn local para indexar o PlayerId (enquanto não há handshake)
/// - NÃO inclui o ClientInputSystem headless; GodotInputSystem cuida do input real
/// </summary>
public sealed class GodotClientSimulationBuilder(IServiceProvider rootProvider) : BaseSimulationBuilder<ClientResourceContext>
{
    protected override ClientResourceContext CreateResourceContext(World world)
        => new ClientResourceContext(rootProvider, world);

    protected override ISystem<float> RegisterComponentUpdate(World world, ClientResourceContext resources)
    {
        var syncSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentUpdate<Position>(),
            resources.PlayerNet.RegisterComponentUpdate<Direction>(),
            resources.PlayerNet.RegisterComponentUpdate<Health>(),
            resources.PlayerNet.RegisterComponentUpdate<PlayerState>(),
        };
        return new Group<float>("Net Update Systems", syncSystems);
    }

    protected override ISystem<float> RegisterComponentPost(World world, ClientResourceContext resources)
    {
        var options = new SyncOptions(
            SyncFrequency.OneShot,
            SyncTarget.Server,
            NetworkDeliveryMethod.ReliableOrdered,
            NetworkChannel.Simulation,
            0);

        var postSystems = new ISystem<float>[]
        {
            resources.PlayerNet.RegisterComponentPost<MoveIntent>(options),
            resources.PlayerNet.RegisterComponentPost<AttackIntent>(options),
        };
        return new Group<float>("Net Post Systems", postSystems);
    }

    protected override ISystem<float> CreateSystems(World world, ClientResourceContext resources)
    {
        var systems = new ISystem<float>[]
        {
            new DevTestSpawnSystem(world, resources.PlayerFactory, new Logger<DevTestSpawnSystem>(resources.LoggerFactory)),
        };
        return new Group<float>("Main Systems", systems);
    }
}