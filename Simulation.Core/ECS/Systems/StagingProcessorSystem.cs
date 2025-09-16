using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Staging.Map;
using Simulation.Core.ECS.Staging.Player;

namespace Simulation.Core.ECS.Systems;

 [PipelineSystem(SystemStage.Staging)]
 [DependsOn(typeof(NetworkSystem))]
public class StagingProcessorSystem(World world,
    IWorldStaging staging,
    EntityIndexSystem entityIndex) : BaseSystem<World, float>(world)
{
    
    public override void Update(in float deltaTime)
    {
        // Processa mapas pendentes
    while (staging.TryDequeue<MapData>(StagingQueue.MapLoaded, out var mapData))
        {
            if (entityIndex.TryGetMap(mapData.MapId, out _)) 
                continue; // Mapa já existe
            
            World.Create<NewlyCreated, MapData>(new NewlyCreated(), mapData);
        }
        
        // Processa logins pendentes
    while (staging.TryDequeue<PlayerData>(StagingQueue.PlayerLogin, out var playerData))
        {
            if (entityIndex.TryGetPlayerEntity(playerData.Id, out _)) 
                continue; // Jogador já está online
            
            World.Create<NewlyCreated, PlayerData>(new NewlyCreated(), playerData);
        }

        // Processa saídas pendentes
    while (staging.TryDequeue<int>(StagingQueue.PlayerLeave, out var charId))
        {
            if (!entityIndex.TryGetPlayerEntity(charId, out var entity)) 
                continue; // Jogador não encontrado
            
            World.Add<NewlyDestroyed>(entity, new NewlyDestroyed());
        }
    }
}