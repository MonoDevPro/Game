using Arch.Core;
using Arch.System;
using Simulation.Core.Network;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.Network.Contracts;

namespace Simulation.Core.ECS.Shared.Systems;

/// <summary>
/// Sistema dedicado a processar todos os eventos de rede de entrada uma vez por frame.
/// Deve ser o primeiro sistema a ser executado na pipeline.
/// </summary>
 [PipelineSystem(SystemStage.Net)]
public class NetworkSystem(World world, INetworkManager networkManager) : BaseSystem<World, float>(world)
{
    public override void Initialize()
    {
        networkManager.Initialize();
    }

    public override void BeforeUpdate(in float t)
    {
        // Esta é a única chamada para PollEvents em todo o loop de simulação.
        networkManager.PollEvents();
    }
    
    public override void Dispose()
    {
        networkManager.Stop();
    }
}