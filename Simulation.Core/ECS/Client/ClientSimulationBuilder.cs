// Em Simulation.Core/ECS/Client/ClientSimulationBuilder.cs

using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Client.Systems;
using Simulation.Core.ECS.Server.Systems;
using Simulation.Core.ECS.Server.Systems.Indexes;
using Simulation.Core.Network;
using Simulation.Generated.Network;

namespace Simulation.Core.ECS.Client;

public class ClientSimulationBuilder
{
    public Group<float> Build(IServiceProvider rootServices)
    {
        var world = World.Create();
        var services = new ServiceCollection();
        
        services.AddSingleton(world);
        services.AddSingleton(rootServices.GetRequiredService<NetworkManager>());

        // O cliente também precisa de um índice para encontrar entidades
        services.AddSingleton<IPlayerIndex, EntityIndexSystem>(); 
        services.AddSingleton<IMapIndex, EntityIndexSystem>();
        services.AddSingleton<EntityIndexSystem>();

        // Fábricas para criar entidades no cliente
        services.AddSingleton<IEntityFactory, EntityFactory>();

        // Sistema gerado para receber inputs e enviar para o servidor
        services.AddSingleton<GeneratedClientIntentSystem>();
        services.AddSingleton<RenderSystem>(); // O seu sistema de "renderização"

        var provider = services.BuildServiceProvider();
        var pipeline = new Group<float>("Client Systems");
        
        pipeline.Add(provider.GetRequiredService<WorldIndexSystem>());
        pipeline.Add(provider.GetRequiredService<GeneratedClientIntentSystem>());
        pipeline.Add(provider.GetRequiredService<RenderSystem>());

        pipeline.Initialize();
        return pipeline;
    }
}