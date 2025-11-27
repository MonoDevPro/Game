using Arch.Core;
using Game.ECS.Components;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Services;

/// <summary>
/// Sistema responsável por sincronizar mudanças de Position com o MapSpatial.
/// Usa componente PositionChanged para detectar mudanças de forma eficiente.
/// 
/// Vantagens:
/// - Só processa entidades que realmente mudaram (query otimizada)
/// - Não precisa cache/dictionary de posições anteriores
/// - Remove automaticamente o componente após processar
/// - Zero overhead para entidades estáticas
/// 
/// Autor: MonoDevPro
/// Data: 2025-01-15
/// </summary>
public sealed class SpatialService(IMapService mapService, ILogger<SpatialService>? logger = null)
{
    public void RegisterSpatial(Entity entity, Position position, sbyte floor, int mapId)
    {
        var spatial = mapService.GetMapSpatial(mapId);
        
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
        
        var spatial = mapService.GetMapSpatial(mapId);
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
    public void UnregisterSpatial(Entity entity, Position position, sbyte floor, int mapId)
    {
        var spatial = mapService.GetMapSpatial(mapId);
        
        var spatialPosition = new SpatialPosition(position.X, position.Y, floor);
        spatial.Remove(spatialPosition, entity);
        
        logger?.LogDebug(
            "[SpatialSync] Entity {Entity} removed from spatial at ({X}, {Y}, {Z})",
            entity.Id, spatialPosition.X, spatialPosition.Y, spatialPosition.Floor);
    }
}