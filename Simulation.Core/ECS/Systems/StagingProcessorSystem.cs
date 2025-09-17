using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Staging;

namespace Simulation.Core.ECS.Systems;

[PipelineSystem(SystemStage.Staging)]
[DependsOn(typeof(NetworkSystem))]
public class StagingProcessorSystem(Group<float> container, World world, IWorldStaging staging) : BaseSystem<World, float>(world)
{
    private readonly EntityFactorySystem _factorySystem = container.Get<EntityFactorySystem>();
    
    public override void Update(in float deltaTime)
    {
        // Processa logins pendentes
        while (staging.TryDequeue<PlayerData>(StagingQueue.PlayerLogin, out var playerData))
        {
            if (!_factorySystem.TryCreate(playerData, out var entity) || entity == null)
                continue; // Falha ao criar entidade do jogador
        }

        // Processa saídas pendentes
        while (staging.TryDequeue<int>(StagingQueue.PlayerLeave, out var charId))
        {
            if (!_factorySystem.TryDestroy(charId, out PlayerData playerData) || playerData == default)
                continue; // Falha ao destruir entidade do jogador
        }
    }
}