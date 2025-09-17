using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Sync;
using Simulation.Core.Network.Contracts;

namespace Simulation.Core.ECS.Builders;

public class GroupSystems : ISystem<float>
{
    // O grupo interno que contém todos os sistemas da pipeline.
    private readonly Group<float> _groupSystems;

    public GroupSystems(IServiceProvider provider, World world, WorldManager worldManager, bool isServer)
    {
        _groupSystems = new Group<float>(isServer ? "ServerSystems" : "ClientSystems");
        
        var systemServices = new ServiceCollection();
        systemServices.AddSingleton(_groupSystems);
        systemServices.AddSingleton(world);
        systemServices.AddSingleton(worldManager.WorldSpatial);
        systemServices.AddSingleton(worldManager.MapService);
        
        systemServices.AddSingleton(provider.GetRequiredService<ILoggerFactory>());
        systemServices.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        systemServices.AddSingleton(provider.GetRequiredService<IWorldStaging>());
        systemServices.AddSingleton(provider.GetRequiredService<INetworkManager>());
        
        var endpoint = provider
            .GetRequiredService<IChannelProcessorFactory>()
            .CreateOrGet(NetworkChannel.Simulation);
        systemServices.AddSingleton(endpoint);
        
        var ecsServiceProvider = systemServices.BuildServiceProvider();
        
        // Regista os sistemas usando o método de extensão na instância interna.
        _groupSystems.RegisterAttributedSystems(ecsServiceProvider, isServer);
        
        _groupSystems.RegisterAttributedSyncSystems(ecsServiceProvider);
    }

    /// <summary>
    /// Inicializa todos os sistemas registados no grupo.
    /// </summary>
    public void Initialize()
    {
        _groupSystems.Initialize();
    }

    /// <summary>
    /// O método BeforeUpdate é delegado para o grupo interno.
    /// </summary>
    public void BeforeUpdate(in float t)
    {
        _groupSystems.BeforeUpdate(in t);
    }

    /// <summary>
    /// O método Update agora orquestra o ciclo de vida completo para todos os sistemas.
    /// Chama BeforeUpdate, Update e AfterUpdate em sequência.
    /// </summary>
    public void Update(in float t)
    {
        // 1. Chama BeforeUpdate para todos os sistemas.
        _groupSystems.BeforeUpdate(in t);
        
        // 2. Chama Update para todos os sistemas.
        _groupSystems.Update(in t);
        
        // 3. Chama AfterUpdate para todos os sistemas.
        _groupSystems.AfterUpdate(in t);
    }
    
    /// <summary>
    /// O método AfterUpdate é delegado para o grupo interno.
    /// </summary>
    public void AfterUpdate(in float t)
    {
        _groupSystems.AfterUpdate(in t);
    }

    /// <summary>
    /// Liberta os recursos de todos os sistemas registados.
    /// </summary>
    public void Dispose()
    {
        _groupSystems.Dispose();
    }
}