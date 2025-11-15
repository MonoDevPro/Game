using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

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
public sealed partial class SpatialSyncSystem(
    World world, 
    IMapService mapService, 
    ILogger<SpatialSyncSystem>? logger = null)
    : GameSystem(world)
{
    /// <summary>
    /// Processa entidades que mudaram de posição.
    /// Query só retorna entidades com PositionChanged - muito eficiente!
    /// </summary>
    [Query]
    [All<PositionChanged, MapId>]
    private void BatchSyncPositionChanges(
        in Entity entity,
        in PositionChanged change,
        in MapId mapId)
    {
        var spatial = mapService.GetMapSpatial(mapId.Value);

        // Se é primeira inserção (oldPosition == default)
        if (change.OldPosition.X == 0 && 
            change.OldPosition.Y == 0 && 
            change.OldPosition.Z == 0)
        {
            spatial.Insert(change.NewPosition, entity);
            
            logger?.LogDebug(
                "[SpatialSync] Entity {Entity} inserted at ({X}, {Y}, {Z})",
                entity.Id, change.NewPosition.X, change.NewPosition.Y, change.NewPosition.Z);
        }
        else
        {
            // Atualização de posição
            if (!spatial.Update(change.OldPosition, change.NewPosition, entity))
            {
                // Fallback: remove e reinsere
                spatial.Remove(change.OldPosition, entity);
                spatial.Insert(change.NewPosition, entity);
                
                logger?.LogDebug(
                    "[SpatialSync] Entity {Entity} relocated from ({OldX}, {OldY}, {OldZ}) to ({NewX}, {NewY}, {NewZ})",
                    entity.Id,
                    change.OldPosition.X, change.OldPosition.Y, change.OldPosition.Z,
                    change.NewPosition.X, change.NewPosition.Y, change.NewPosition.Z);
            }
            else
            {
                logger?.LogDebug(
                    "[SpatialSync] Entity {Entity} updated from ({OldX}, {OldY}, {OldZ}) to ({NewX}, {NewY}, {NewZ})",
                    entity.Id,
                    change.OldPosition.X, change.OldPosition.Y, change.OldPosition.Z,
                    change.NewPosition.X, change.NewPosition.Y, change.NewPosition.Z);
            }
        }

        // Remove o componente temporário após processar
        World.Remove<PositionChanged>(entity);
    }
    
    public void OnEntityCreated(Entity entity, Position position, int mapId)
    {
        var spatial = mapService.GetMapSpatial(mapId);
        spatial.Insert(position, entity);
        
        logger?.LogDebug(
            "[SpatialSync] Entity {Entity} inserted into spatial at ({X}, {Y}, {Z})",
            entity.Id, position.X, position.Y, position.Z);
    }
    

    /// <summary>
    /// Remove entidade do spatial quando destruída.
    /// </summary>
    public void OnEntityDestroyed(Entity entity, Position position, int mapId)
    {
        var spatial = mapService.GetMapSpatial(mapId);
        spatial.Remove(position, entity);
        
        logger?.LogDebug(
            "[SpatialSync] Entity {Entity} removed from spatial at ({X}, {Y}, {Z})",
            entity.Id, position.X, position.Y, position.Z);
    }
}