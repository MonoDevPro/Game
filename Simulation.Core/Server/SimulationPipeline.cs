using Arch.Core;
using Arch.System;
using LiteNetLib;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Server.Staging;
using Simulation.Core.Server.Systems;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Options;
using Simulation.Core.Shared.Utils.Map;
using Simulation.Generated.Network;

namespace Simulation.Core.Server;

public sealed class SimulationPipeline : Group<float>
{
    public SimulationPipeline(WorldOptions worldOptions, SpatialOptions spatialOptions, IServiceProvider mainAppServices) 
        : base("SimulationServer Group")
    {
        // 1. Crie o World explicitamente primeiro
        var world = World.Create(
            chunkSizeInBytes: worldOptions.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: worldOptions.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: worldOptions.ArchetypeCapacity,
            entityCapacity: worldOptions.EntityCapacity
        );

        var systemServices = new ServiceCollection();

        // 2. Registre a instância do World e os serviços externos
        systemServices.AddSingleton(world);
        systemServices.AddSingleton(mainAppServices.GetRequiredService<IPlayerStagingArea>());
        systemServices.AddSingleton(mainAppServices.GetRequiredService<MapManagerService>());
        systemServices.AddSingleton(mainAppServices.GetRequiredService<NetManager>()); // Exemplo de outro serviço
        systemServices.AddSingleton(mainAppServices.GetRequiredService<NetworkManager>()); // Exemplo de outro serviço
        
        
        // 3. Registre todos os sistemas como Singletons
        // Para sistemas com dependências primitivas, use um factory lambda.
        systemServices.AddSingleton(sp => new SpatialIndexSystem(
            sp.GetRequiredService<World>(), 
            spatialOptions.Width,
            spatialOptions.Height
        ));
        
        // Sistemas com dependências que já estão no contêiner podem ser registrados diretamente.
        systemServices.AddSingleton<NetworkSystem>();
        systemServices.AddSingleton<MapIndexSystem>();
        systemServices.AddSingleton<PlayerIndexSystem>();
        systemServices.AddSingleton<SpatialIndexSystem>();
        systemServices.AddSingleton<PlayerLifecycleSystem>();
        systemServices.AddSingleton<MovementSystem>();
        systemServices.AddSingleton<GeneratedServerSyncSystem>();
        systemServices.AddSingleton<MapManagerService>();
        
        // 4. Constrói o provedor de serviços exclusivo para o ECS
        var ecsServiceProvider = systemServices.BuildServiceProvider();

        // 5. Método auxiliar para adicionar e registrar ao mesmo tempo
        void AddSystem<T>() where T : ISystem<float>
        {
            Add(ecsServiceProvider.GetRequiredService<T>());
        }

        // 6. Adiciona os sistemas ao grupo na ordem de execução correta
        //    usando o método auxiliar para manter o código limpo (DRY).
        
        AddSystem<NetworkSystem>();
        AddSystem<MapIndexSystem>();
        AddSystem<PlayerIndexSystem>();
        AddSystem<SpatialIndexSystem>();
        AddSystem<PlayerLifecycleSystem>();
        AddSystem<MovementSystem>();
        AddSystem<GeneratedServerSyncSystem>();
        
        // 7. Inicializa todos os sistemas
        Initialize();
    }
}