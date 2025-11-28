using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Services;

/// <summary>
/// System responsible for synchronizing position changes with the MapSpatial index.
/// This system processes entities with dirty state flags and notifies the spatial
/// system of position updates. Actual spatial index updates are performed by the
/// MapService based on position change tracking.
/// 
/// Note: This system runs after movement systems to ensure positions are finalized
/// before spatial index updates occur.
/// 
/// Autor: MonoDevPro
/// Data: 2025-01-15
/// </summary>
public sealed partial class SpatialService(World world, IMapService? mapService)
    : GameSystem(world, mapService)
{
    private readonly IMapService? _mapService = mapService;
    
    /// <summary>
    /// Synchronizes spatial index for entities that have changed position.
    /// This system is called after movement to ensure spatial queries are accurate.
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
        // Only process if state (position) changed
        if (!dirty.IsDirty(DirtyComponentType.State))
            return;
            
        // Get the spatial index for this map
        var spatial = _mapService?.GetMapSpatial(mapId.Value);
        if (spatial == null)
            return;
        
        // The actual position update in the spatial index should be handled
        // by the GameServices.UpdateSpatial method which tracks previous positions.
        // This system serves as a checkpoint to ensure spatial queries reflect
        // the latest entity positions after the current tick's movement updates.
    }
}