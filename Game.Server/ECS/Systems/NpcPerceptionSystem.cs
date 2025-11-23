using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class NpcPerceptionSystem(
    World world,
    IMapService mapService,
    ILogger<NpcPerceptionSystem>? logger = null)
    : GameSystem(world)
{
    private const int MaxSpatialResults = 64;

    [Query]
    [All<AIControlled, Position, MapId, NpcBehavior, NpcTarget>]
    private void UpdatePerception(
        in Entity entity,
        in Position position,
        in MapId mapId,
        in NpcBehavior behavior,
        in Floor floor,
        ref NpcTarget target)
    {
        if (behavior.Type == NpcBehaviorType.Passive)
            return;
        
        if (target.HasTarget)
        {
            if (!World.IsAlive(target.Target) 
                || !World.Has<PlayerControlled>(target.Target) 
                || World.Has<Dead>(target.Target) 
                || !World.TryGet(target.Target, out MapId targetMap) 
                || targetMap.Value != mapId.Value
                || !World.TryGet(target.Target, out Floor targetFloor)
                || targetFloor.Level != floor.Level
                || !World.TryGet(target.Target, out Position targetPosition))
            {
                target.Clear();
                return;
            }
            target.LastKnownPosition = targetPosition;
            target.DistanceSquared = DistanceSquared(position, targetPosition);
            if (target.TargetNetworkId == -1)  // -1 means NetworkId wasn't resolved yet
                target.TargetNetworkId = TryResolveNetworkId(target.Target);

            float leashRangeSq = behavior.LeashRange * behavior.LeashRange;
            if (target.DistanceSquared > leashRangeSq)
                target.Clear();
        }

        if (target.HasTarget)
            return;

        var spatial = mapService.GetMapSpatial(mapId.Value);
        int radius2D = Math.Max(1, (int)MathF.Ceiling(behavior.VisionRange));
        var min = new SpatialPosition(position.X - radius2D, position.Y - radius2D, floor.Level);
        var max = new SpatialPosition(position.X + radius2D, position.Y + radius2D, floor.Level);

        Span<Entity> results = stackalloc Entity[MaxSpatialResults];
        int found = spatial.QueryArea(min, max, results);

        Entity best = Entity.Null;
        float bestDistance = float.MaxValue;
        Position bestPosition = default;

        for (int i = 0; i < found; i++)
        {
            var candidate = results[i];
            if (candidate == entity) continue;
            if (!World.IsAlive(candidate)) continue;
            if (!World.Has<PlayerControlled>(candidate)) continue;
            if (World.Has<Dead>(candidate)) continue;
            if (!World.TryGet(candidate, out MapId candidateMap) || candidateMap.Value != mapId.Value) continue;
            if (!World.TryGet(candidate, out Floor candidateFloor) || candidateFloor.Level != floor.Level) continue;
            if (!World.TryGet(candidate, out Position candidatePosition)) continue;

            float distanceSq = DistanceSquared(position, candidatePosition);
            if (distanceSq > behavior.VisionRange * behavior.VisionRange)
                continue;

            if (distanceSq >= bestDistance)
                continue;

            best = candidate;
            bestDistance = distanceSq;
            bestPosition = candidatePosition;
        }

        if (best == Entity.Null)
            return;

        int networkId = TryResolveNetworkId(best);
        target.SetTarget(best, networkId, bestPosition, bestDistance);
        logger?.LogDebug("[NpcPerception] NPC found target NetworkId={TargetId} at distance {Distance}", 
            networkId, MathF.Sqrt(bestDistance));
    }

    private int TryResolveNetworkId(in Entity entity)
    {
        return World.TryGet(entity, out NetworkId networkId) ? networkId.Value : -1;
    }

    private static float DistanceSquared(in Position a, in Position b)
    {
        float dx = a.X - b.X;
        float dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}