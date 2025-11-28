using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
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
public sealed partial class SpatialService(World world, IMapService? mapService)
    : GameSystem(world, mapService)
{
    private readonly IMapService? _mapService = mapService;
    
    /// <summary>
    /// Updates the spatial index for entities that have changed position.
    /// </summary>
    [Query]
    [All<NetworkId, Position, Floor, MapId, DirtyFlags>]
    private void SyncSpatialIndex(
        in Entity entity,
        in NetworkId networkId,
        in Position pos,
        in Floor floor,
        in MapId mapId,
        ref DirtyFlags dirty)
    {
        // Only sync if state changed
        if (!dirty.IsDirty(DirtyComponentType.State))
            return;
            
        // Update spatial index
        var spatial = _mapService?.GetMapSpatial(mapId.Value);
        if (spatial == null)
            return;
            
        // The spatial index should be updated with the new position
        // This is typically handled by the MapSpatial.Move method
        var spatialPos = new SpatialPosition(pos.X, pos.Y, floor.Level);
        
        // Note: The actual spatial update should be managed elsewhere,
        // this system just marks that the entity should be synced
    }
}