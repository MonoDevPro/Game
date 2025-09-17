using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Builders;

/// <summary>
/// Uma classe que gere a execução de um grupo de sistemas (pipeline),
/// implementando a interface ISystem<float> para ter um ciclo de vida completo.
/// Esta classe encapsula um Group<float> para orquestrar as chamadas.
/// </summary>
public class PipelineSystems : ISystem<float>
{
    // O grupo interno que contém todos os sistemas da pipeline.
    private readonly Group<float> _systems;
    public readonly World World;

    public PipelineSystems(IServiceProvider provider, WorldOptions options, bool isServer)
    {
        World = World.Create(
            chunkSizeInBytes: options.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: options.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: options.ArchetypeCapacity,
            entityCapacity: options.EntityCapacity);
        
        var systemServices = new ServiceCollection();
        systemServices.AddSingleton(World);
        systemServices.AddSingleton(provider.GetRequiredService<IWorldStaging>());
        systemServices.AddSingleton(provider.GetRequiredService<INetworkManager>());
        systemServices.AddSingleton(provider.GetRequiredService<WorldManager>());
        var endpoint = provider
            .GetRequiredService<IChannelProcessorFactory>()
            .CreateOrGet(NetworkChannel.Simulation);
        systemServices.AddSingleton<IChannelEndpoint>(endpoint);
        systemServices.AddSingleton<Group<float>>(sp => new Group<float>(isServer ? "ServerSystems" : "ClientSystems"));
        
        // Registro dos sistemas
        var ecsServiceProvider = systemServices.BuildServiceProvider();
        
        // Cria a instância interna do grupo de sistemas.
        _systems = ecsServiceProvider.GetRequiredService<Group<float>>();
        
        // Regista os sistemas usando o método de extensão na instância interna.
        _systems.RegisterAttributedSystems<Group<float>>(ecsServiceProvider, isServer);
    }

    /// <summary>
    /// Inicializa todos os sistemas registados no grupo.
    /// </summary>
    public void Initialize()
    {
        _systems.Initialize();
    }

    /// <summary>
    /// O método BeforeUpdate é delegado para o grupo interno.
    /// </summary>
    public void BeforeUpdate(in float t)
    {
        _systems.BeforeUpdate(in t);
    }

    /// <summary>
    /// O método Update agora orquestra o ciclo de vida completo para todos os sistemas.
    /// Chama BeforeUpdate, Update e AfterUpdate em sequência.
    /// </summary>
    public void Update(in float t)
    {
        // 1. Chama BeforeUpdate para todos os sistemas.
        _systems.BeforeUpdate(in t);
        
        // 2. Chama Update para todos os sistemas.
        _systems.Update(in t);
        
        // 3. Chama AfterUpdate para todos os sistemas.
        _systems.AfterUpdate(in t);
    }
    
    /// <summary>
    /// O método AfterUpdate é delegado para o grupo interno.
    /// </summary>
    public void AfterUpdate(in float t)
    {
        _systems.AfterUpdate(in t);
    }

    /// <summary>
    /// Liberta os recursos de todos os sistemas registados.
    /// </summary>
    public void Dispose()
    {
        _systems.Dispose();
    }
}