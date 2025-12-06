using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Schema.Components;
using Game.ECS.Services.Map;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Mantém o índice espacial sincronizado com a posição atual das entidades walkable.
/// Garante inserção inicial e corrige alterações fora do fluxo normal de movimento (teleporte, respawn).
/// </summary>
public sealed partial class SpatialSyncSystem : GameSystem
{
    private readonly IMapIndex _mapIndex;

    public SpatialSyncSystem(World world, IMapIndex mapIndex, ILogger<SpatialSyncSystem>? logger = null)
        : base(world, logger)
    {
        _mapIndex = mapIndex;
    }

    [Query]
    [All<SpatialAnchor, Position, Floor, MapId, Walkable>]
    private void SyncSpatialAnchor(
        in Entity entity,
        in Position position,
        in Floor floor,
        in MapId mapId,
        ref SpatialAnchor anchor)
    {
        // Nenhuma alteração: já está sincronizado
        if (anchor.IsTracked &&
            anchor.MapId == mapId.Value &&
            anchor.Position.Equals(position) &&
            anchor.Floor == floor.Value)
        {
            return;
        }

        if (anchor.IsTracked && _mapIndex.HasMap(anchor.MapId))
        {
            var previousSpatial = _mapIndex.GetMapSpatial(anchor.MapId);
            if (!previousSpatial.Remove(anchor.Position, anchor.Floor, entity))
                LogDebug("[SpatialSync] Anchor removal missed for entity {Entity} at ({X},{Y},{Floor})", entity, anchor.Position.X, anchor.Position.Y, anchor.Floor);
        }

        if (!_mapIndex.HasMap(mapId.Value))
        {
            LogWarning("[SpatialSync] Map {MapId} not registered for entity {Entity}", mapId.Value, entity);
            anchor.IsTracked = false;
            return;
        }

        _mapIndex.GetMapSpatial(mapId.Value).Insert(position, floor.Value, entity);
        anchor.MapId = mapId.Value;
        anchor.Position = position;
        anchor.Floor = floor.Value;
        anchor.IsTracked = true;
    }
}
