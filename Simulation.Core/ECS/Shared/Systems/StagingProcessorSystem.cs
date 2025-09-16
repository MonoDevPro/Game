using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.ECS.Pipeline;
using Simulation.Core.ECS.Shared.Staging;

namespace Simulation.Core.ECS.Shared.Systems;

 [PipelineSystem(SystemStage.Staging)]
 [DependsOn(typeof(Simulation.Core.ECS.Shared.Systems.NetworkSystem))]
public class StagingProcessorSystem(World world,
    IPlayerStagingArea playerStagingArea, IMapStagingArea mapStagingArea,
    EntityIndexSystem entityIndex) : BaseSystem<World, float>(world)
{
    
    public override void Update(in float deltaTime)
    {
        // Processa mapas pendentes
        while (mapStagingArea.TryDequeueMapLoaded(out var mapData))
        {
            if (entityIndex.TryGetMap(mapData.MapId, out _)) 
                continue; // Mapa já existe
            
            World.Create<NewlyCreated, MapData>(new NewlyCreated(), mapData);
        }
        
        // Processa logins pendentes
        while (playerStagingArea.TryDequeueLogin(out var playerData))
        {
            if (entityIndex.TryGetPlayerEntity(playerData.Id, out _)) 
                continue; // Jogador já está online
            
            World.Create<NewlyCreated, PlayerData>(new NewlyCreated(), playerData);
        }

        // Processa saídas pendentes
        while (playerStagingArea.TryDequeueLeave(out var charId))
        {
            if (!entityIndex.TryGetPlayerEntity(charId, out var entity)) 
                continue; // Jogador não encontrado
            
            World.Add<NewlyDestroyed>(entity, new NewlyDestroyed());
        }
    }
}