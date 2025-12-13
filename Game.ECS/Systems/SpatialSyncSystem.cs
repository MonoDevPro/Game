using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Services.Map;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Mantém o índice espacial sincronizado com a posição atual das entidades walkable.
/// Garante inserção inicial e corrige alterações fora do fluxo normal de movimento (teleporte, respawn).
/// </summary>
public sealed partial class SpatialSyncSystem : GameSystem
{
    private readonly MapIndex _mapIndex;

    public SpatialSyncSystem(World world, MapIndex mapIndex, ILogger<SpatialSyncSystem>? logger = null)
        : base(world, logger)
    {
        _mapIndex = mapIndex;
    }

    [Query]
    [All<SpatialAnchor, Position, MapId, Walkable>]
    private void SyncSpatialAnchor(
        in Entity entity,
        in Position position,
        in MapId mapId,
        ref SpatialAnchor anchor)
    {
        // Nenhuma alteração: já está sincronizado
        if (anchor.IsTracked &&
            anchor.MapId == mapId.Value &&
            anchor.Position.Equals(position))
        {
            return;
        }

        if (anchor.IsTracked && _mapIndex.HasMap(anchor.MapId))
        {
            var previousSpatial = _mapIndex.GetMapSpatial(anchor.MapId);
            if (!previousSpatial.Remove(anchor.Position, entity))
                LogDebug("[SpatialSync] Anchor removal missed for entity {Entity} at ({X},{Y},{Z})", entity, anchor.Position.X, anchor.Position.Y, anchor.Position.Z);
        }

        if (!_mapIndex.HasMap(mapId.Value))
        {
            LogWarning("[SpatialSync] Map {MapId} not registered for entity {Entity}", mapId.Value, entity);
            anchor.IsTracked = false;
            return;
        }

        _mapIndex.GetMapSpatial(mapId.Value).Insert(position, entity);
        anchor.MapId = mapId.Value;
        anchor.Position = position;
        anchor.IsTracked = true;
    }
}
