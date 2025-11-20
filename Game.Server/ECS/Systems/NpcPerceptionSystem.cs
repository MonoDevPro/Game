using System;
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
        ref NpcTarget target)
    {
        if (!mapService.HasMap(mapId.Value))
            return;

        ValidateCurrentTarget(ref target, in position, in behavior, mapId.Value);

        if (behavior.Type == NpcBehaviorType.Passive)
            return;

        if (target.HasTarget)
            return;

        var spatial = mapService.GetMapSpatial(mapId.Value);
        int radius = Math.Max(1, (int)MathF.Ceiling(behavior.VisionRange));
        var area = new AreaPosition(
            position.X - radius,
            position.Y - radius,
            position.Z,
            position.X + radius,
            position.Y + radius,
            position.Z);

        Span<Entity> results = stackalloc Entity[MaxSpatialResults];
        int found = spatial.QueryArea(area, results);

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

        target.Target = best;
        target.LastKnownPosition = bestPosition;
        target.DistanceSquared = bestDistance;
        target.TargetNetworkId = TryResolveNetworkId(best);
        logger?.LogDebug("[NpcPerception] NPC found target {TargetId} at distance {Distance}", target.TargetNetworkId, MathF.Sqrt(bestDistance));
    }

    private void ValidateCurrentTarget(ref NpcTarget target, in Position origin, in NpcBehavior behavior, int mapId)
    {
        if (!target.HasTarget)
            return;

        if (!World.IsAlive(target.Target) ||
            !World.Has<PlayerControlled>(target.Target) ||
            World.Has<Dead>(target.Target) ||
            !World.TryGet(target.Target, out MapId targetMap) ||
            targetMap.Value != mapId ||
            !World.TryGet(target.Target, out Position targetPosition))
        {
            target.Clear();
            return;
        }

        target.LastKnownPosition = targetPosition;
        target.DistanceSquared = DistanceSquared(origin, targetPosition);
        if (target.TargetNetworkId == 0)
            target.TargetNetworkId = TryResolveNetworkId(target.Target);

        float leashRangeSq = behavior.LeashRange * behavior.LeashRange;
        if (target.DistanceSquared > leashRangeSq)
            target.Clear();
    }

    private int TryResolveNetworkId(in Entity entity)
    {
        return World.TryGet(entity, out NetworkId networkId) ? networkId.Value : 0;
    }

    private static float DistanceSquared(in Position a, in Position b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        return dx * dx + dy * dy;
    }
}
