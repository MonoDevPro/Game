using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Sync;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Pipeline;

public class GroupSystems : ISystem<float>
{
    // O grupo interno que contém todos os sistemas da pipeline.
    private readonly Group<float> _groupSystems;

    public GroupSystems(ILoggerFactory factoryLogger, World world, WorldManager worldManager, AuthorityOptions authorityOptions)
    {
        bool isServer = authorityOptions.Authority == Authority.Server;
        
        _groupSystems = new Group<float>(isServer ? "ServerSystems" : "ClientSystems");
        
        var systemServices = new ServiceCollection();
        systemServices.AddSingleton(factoryLogger);
        systemServices.AddSingleton(typeof(ILogger<>), typeof(Logger<>));
        systemServices.AddSingleton(worldManager.NetworkManager);
        systemServices.AddSingleton(worldManager.SimulationEndpoint);
        systemServices.AddSingleton(worldManager.WorldSpatial);
        systemServices.AddSingleton(worldManager.MapService);
        systemServices.AddSingleton(worldManager.WorldSaver);
        systemServices.AddSingleton(worldManager.WorldStaging);
        systemServices.AddSingleton(_groupSystems);
        systemServices.AddSingleton(world);

        var ecsServiceProvider = systemServices.BuildServiceProvider();
        
        _groupSystems.RegisterAttributedSystems(ecsServiceProvider, isServer);
        _groupSystems.RegisterAttributedSyncSystems(ecsServiceProvider, isServer);
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