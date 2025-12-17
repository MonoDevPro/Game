using System.Collections.Generic;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Services.Map;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Converts high-level navigation intent (NavigationAgent.Destination) into low-level movement (Direction).
/// Uses A* pathfinding on the map grid, taking static collisions and current spatial occupancy into account.
/// </summary>
public sealed partial class NavigationSystem(
    World world,
    MapIndex mapIndex,
    ILogger<NavigationSystem>? logger = null)
    : GameSystem(world, logger)
{
    private const int MaxExpandedNodes = 4096;
    private readonly Position[] _neighbors = new Position[8];

    [Query]
    [All<NavigationAgent, Position, MapId, Direction, Speed, Walkable>]
    [None<Dead>]
    private void Navigate(
        in Entity entity,
        ref NavigationAgent agent,
        ref Direction direction,
        ref Speed speed,
        in Walkable walkable,
        in Position position,
        in MapId mapId)
    {
        if (agent.Destination is not { } rawDestination)
            return;

        if (!mapIndex.HasMap(mapId.Value))
        {
            StopAndClear(ref agent, ref direction, ref speed);
            return;
        }

        var grid = mapIndex.GetMapGrid(mapId.Value);
        var destination = grid.ClampToBounds(rawDestination);
        agent.Destination = destination;

        // Reached (within stopping distance)
        if (WithinStoppingDistance(position, destination, agent.StoppingDistance))
        {
            StopAndClear(ref agent, ref direction, ref speed);
            return;
        }

        // Ensure we have a cached path component
        if (!World.Has<NavigationPath>(entity))
        {
            World.Add(entity, new NavigationPath { Target = null, Steps = null, NextIndex = 0 });
        }
        ref var path = ref World.Get<NavigationPath>(entity);

        // Select a reachable goal (destination may be occupied, e.g., chasing a player)
        if (!TrySelectGoal(mapId.Value, in grid, in position, in destination, agent.StoppingDistance, entity, out var goal))
        {
            logger?.LogDebug("[Nav] No reachable goal for entity {Entity} destination ({X},{Y},{Z}) on map {MapId}",
                entity, destination.X, destination.Y, destination.Z, mapId.Value);
            StopAndClear(ref agent, ref direction, ref speed);
            path.Target = null;
            path.Steps = null;
            path.NextIndex = 0;
            return;
        }

        // Rebuild path if needed
        if (agent.IsPathPending || path.Target is null || !path.Target.Value.Equals(goal) || path.Steps is null || path.NextIndex >= path.Steps.Length)
        {
            var steps = FindPathAStar(mapId.Value, in grid, start: position, goal: goal, self: entity);
            agent.IsPathPending = false;

            if (steps is null || steps.Length == 0)
            {
                // No path found
                logger?.LogDebug("[Nav] A* failed for entity {Entity} start ({SX},{SY},{SZ}) goal ({GX},{GY},{GZ}) map {MapId}",
                    entity, position.X, position.Y, position.Z, goal.X, goal.Y, goal.Z, mapId.Value);
                StopAndClear(ref agent, ref direction, ref speed);
                path.Target = null;
                path.Steps = null;
                path.NextIndex = 0;
                return;
            }

            path.Target = goal;
            path.Steps = steps;
            path.NextIndex = 0;
        }

        // Advance if we're already on the next step (e.g., after teleport)
        if (path.Steps is not null)
        {
            while (path.NextIndex < path.Steps.Length && path.Steps[path.NextIndex].Equals(position))
            {
                path.NextIndex++;
            }
        }

        if (path.Steps is null || path.NextIndex >= path.Steps.Length)
        {
            StopAndClear(ref agent, ref direction, ref speed);
            return;
        }

        var next = path.Steps[path.NextIndex];

        // If next step is blocked now (dynamic obstacle), request a repath.
        if (mapIndex.ValidateMove(mapId.Value, next, entity) != MovementResult.Allowed)
        {
            agent.IsPathPending = true;
            direction.X = 0;
            direction.Y = 0;
            return;
        }

        // Set movement direction toward the next step
        direction.X = (sbyte)System.Math.Sign(next.X - position.X);
        direction.Y = (sbyte)System.Math.Sign(next.Y - position.Y);

        // Ensure speed is non-zero when navigating (AI normally sets it; scripts might not)
        if (speed.Value <= 0f)
        {
            speed.Value = walkable.BaseSpeed + walkable.CurrentModifier;
        }

        // If the next step is adjacent and we are moving toward it, MovementSystem will attempt it.
    }

    private static bool WithinStoppingDistance(in Position from, in Position to, float stoppingDistance)
    {
        if (stoppingDistance <= 0f)
            return from.Equals(to);

        float dx = from.X - to.X;
        float dy = from.Y - to.Y;
        float dist = System.MathF.Sqrt(dx * dx + dy * dy);
        return dist <= stoppingDistance;
    }

    private static void StopAndClear(ref NavigationAgent agent, ref Direction direction, ref Speed speed)
    {
        agent.Destination = null;
        agent.IsPathPending = false;
        direction.X = 0;
        direction.Y = 0;
        speed.Value = 0f;
    }

    private bool TrySelectGoal(
        int mapId,
        in IMapGrid grid,
        in Position start,
        in Position destination,
        float stoppingDistance,
        Entity self,
        out Position goal)
    {
        // If we can stand on destination, use it.
        if (mapIndex.ValidateMove(mapId, destination, self) == MovementResult.Allowed)
        {
            goal = destination;
            return true;
        }

        // If we're already close enough, don't require a goal tile.
        if (WithinStoppingDistance(start, destination, stoppingDistance))
        {
            goal = start;
            return true;
        }

        // Otherwise, pick the closest walkable tile within stopping distance.
        int radius = System.Math.Max(1, (int)System.Math.Ceiling(stoppingDistance));
        bool found = false;
        int bestScore = int.MaxValue;
        Position best = default;

        for (int dx = -radius; dx <= radius; dx++)
        {
            for (int dy = -radius; dy <= radius; dy++)
            {
                float distToDest = System.MathF.Sqrt(dx * dx + dy * dy);
                if (distToDest > stoppingDistance)
                    continue;

                var candidate = new Position { X = destination.X + dx, Y = destination.Y + dy, Z = destination.Z };
                if (!grid.InBounds(candidate))
                    continue;

                if (mapIndex.ValidateMove(mapId, candidate, self) != MovementResult.Allowed)
                    continue;

                int score = System.Math.Abs(candidate.X - start.X) + System.Math.Abs(candidate.Y - start.Y);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = candidate;
                    found = true;
                }
            }
        }

        goal = best;
        return found;
    }

    private Position[]? FindPathAStar(int mapId, in IMapGrid grid, Position start, Position goal, Entity self)
    {
        if (start.Equals(goal))
            return [];

        var open = new PriorityQueue<Position, int>();
        var cameFrom = new Dictionary<Position, Position>();
        var gScore = new Dictionary<Position, int> { [start] = 0 };
        var closed = new HashSet<Position>();

        open.Enqueue(start, priority: Heuristic(start, goal));

        int expanded = 0;
        while (open.TryDequeue(out var current, out _))
        {
            if (closed.Contains(current))
                continue;

            if (current.Equals(goal))
            {
                return ReconstructPath(cameFrom, start, goal);
            }

            closed.Add(current);

            if (++expanded > MaxExpandedNodes)
                return null;

            int neighborCount = grid.GetWalkableNeighbors(current, _neighbors, allowDiagonal: false);
            for (int i = 0; i < neighborCount; i++)
            {
                var neighbor = _neighbors[i];

                // Dynamic occupancy check (and bounds/collision).
                if (mapIndex.ValidateMove(mapId, neighbor, self) != MovementResult.Allowed)
                    continue;

                int tentativeG = gScore[current] + 1;
                if (gScore.TryGetValue(neighbor, out int existingG) && tentativeG >= existingG)
                    continue;

                cameFrom[neighbor] = current;
                gScore[neighbor] = tentativeG;

                int fScore = tentativeG + Heuristic(neighbor, goal);
                open.Enqueue(neighbor, fScore);
            }
        }

        return null;
    }

    private static int Heuristic(in Position a, in Position b)
    {
        // Manhattan distance (4-direction grid)
        return System.Math.Abs(a.X - b.X) + System.Math.Abs(a.Y - b.Y);
    }

    private static Position[] ReconstructPath(Dictionary<Position, Position> cameFrom, Position start, Position goal)
    {
        // Build from goal back to start
        var reversed = new List<Position>(32) { goal };
        var current = goal;

        while (!current.Equals(start) && cameFrom.TryGetValue(current, out var previous))
        {
            current = previous;
            reversed.Add(current);
        }

        reversed.Reverse();

        // Exclude the start position; remaining entries are steps to follow.
        if (reversed.Count <= 1)
            return [];

        var steps = new Position[reversed.Count - 1];
        for (int i = 1; i < reversed.Count; i++)
            steps[i - 1] = reversed[i];

        return steps;
    }
}
