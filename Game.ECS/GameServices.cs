using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Services;
using Microsoft.Extensions.Logging;

namespace Game.ECS;

public sealed class GameServices(ILogger<GameServices>? logger = null)
{
    public IMapService MapService { get; } = new MapService();
    
    public IPlayerIndex PlayerIndex { get; } = new PlayerIndex();
    
    public INpcIndex NpcIndex { get; } = new NpcIndex();
    
    public GameResources Resources { get; } = new GameResources();
    
    public bool TryGetAnyEntity(int networkId, out Entity entity) => 
        PlayerIndex.TryGetPlayerEntity(networkId, out entity) || NpcIndex.TryGetNpcEntity(networkId, out entity);
    
    public Entity RegisterEntity(Entity entity, Position position, sbyte floor, int mapId)
    {
        RegisterSpatial(entity, position, floor, mapId);
        return entity;
    }
    
    private void RegisterSpatial(Entity entity, Position position, sbyte floor, int mapId)
    {
        var spatial = MapService.GetMapSpatial(mapId);
        
        var spatialPosition = new SpatialPosition(position.X, position.Y, floor);
        spatial.Insert(spatialPosition, entity);
        
        logger?.LogDebug(
            "[SpatialSync] Entity {Entity} inserted into spatial at ({X}, {Y}, {Z})",
            entity.Id, spatialPosition.X, spatialPosition.Y, spatialPosition.Floor);
    }
    
    public void UpdateSpatial(Entity entity, Position oldPos, Position newPos, sbyte floor, int mapId)
    {
        var oldPosition = new SpatialPosition(oldPos.X, oldPos.Y, floor);
        var newPosition = new SpatialPosition(newPos.X, newPos.Y, floor);
        
        var spatial = MapService.GetMapSpatial(mapId);
        if (!spatial.Update(oldPosition, newPosition, entity))
        {
            // Fallback: remove e reinsere
            spatial.Remove(oldPosition, entity);
            spatial.Insert(newPosition, entity);
            
            logger?.LogDebug(
                "[SpatialSync] Entity {Entity} relocated from ({OldX}, {OldY}, {OldZ}) to ({NewX}, {NewY}, {NewZ})",
                entity.Id,
                oldPosition.X, oldPosition.Y, oldPosition.Floor,
                newPosition.X, newPosition.Y, newPosition.Floor);
        }
        else
        {
            logger?.LogDebug(
                "[SpatialSync] Entity {Entity} updated from ({OldX}, {OldY}, {OldZ}) to ({NewX}, {NewY}, {NewZ})",
                entity.Id,
                oldPosition.X, oldPosition.Y, oldPosition.Floor,
                newPosition.X, newPosition.Y, newPosition.Floor);
        }
    }

    /// <summary>
    /// Remove entidade do spatial quando destruída.
    /// </summary>
    private void UnregisterSpatial(Entity entity, Position position, sbyte floor, int mapId)
    {
        var spatial = MapService.GetMapSpatial(mapId);
        
        var spatialPosition = new SpatialPosition(position.X, position.Y, floor);
        spatial.Remove(spatialPosition, entity);
        
        logger?.LogDebug(
            "[SpatialSync] Entity {Entity} removed from spatial at ({X}, {Y}, {Z})",
            entity.Id, spatialPosition.X, spatialPosition.Y, spatialPosition.Floor);
    }
}