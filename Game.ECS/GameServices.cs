using Arch.Core;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Services;

namespace Game.ECS;

public sealed class GameServices
{
    public IMapService MapService { get; } = new MapService();
    
    public IPlayerIndex PlayerIndex { get; } = new PlayerIndex();
    
    public INpcIndex NpcIndex { get; } = new NpcIndex();
    
    public GameResources Resources { get; } = new GameResources();
    
    public SpatialService SpatialService { get; }
    
    public GameServices()
    {
        SpatialService = new SpatialService(MapService, logger: null);
    }
    
    public bool TryGetAnyEntity(int networkId, out Entity entity) => 
        PlayerIndex.TryGetPlayerEntity(networkId, out entity) || NpcIndex.TryGetNpcEntity(networkId, out entity);
}