using System.Diagnostics;
using System.Runtime.CompilerServices;
using Game.ECS.Shared.Components.Navigation;
using Game.ECS.Shared.Core.Navigation;
using Game.ECS.Shared.Data.Navigation;

namespace Game.ECS.Shared.Services.Navigation;

/// <summary>
/// Sistema A* otimizado para grid.
/// Trabalha diretamente com índices de grid.
/// </summary>
public sealed class PathfindingService(NavigationGrid grid, PathfindingPool pool, NavigationConfig? config = null)
{
    private readonly NavigationConfig _config = config ?? NavigationConfig.Default;

    public NavigationGrid Grid => grid;

    /// <summary>
    /// Calcula caminho entre duas posições do grid.
    /// </summary>
    public PathResult FindPath(
        int startX, int startY,
        int goalX, int goalY,
        ref GridPathBuffer pathBuffer,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        var sw = Stopwatch.StartNew();

        // Validação inicial
        if (!grid.IsValidCoord(startX, startY))
            return PathResult.Failed(PathFailReason.InvalidRequest);

        if (!grid.IsValidCoord(goalX, goalY))
            return PathResult.Failed(PathFailReason.InvalidRequest);

        if (!grid.IsWalkable(startX, startY))
            return PathResult.Failed(PathFailReason.StartBlocked);

        if (!grid.IsWalkable(goalX, goalY))
        {
            if (!flags.HasFlag(PathRequestFlags.AllowPartialPath))
                return PathResult.Failed(PathFailReason.GoalBlocked);
        }

        // Já está no destino?
        if (startX == goalX && startY == goalY)
        {
            pathBuffer.Clear();
            pathBuffer.GoalX = goalX;
            pathBuffer.GoalY = goalY;
            return PathResult.Succeeded(0, 0, 0);
        }

        var ctx = pool.Rent();
        try
        {
            var result = FindPathInternal(startX, startY, goalX, goalY, ref pathBuffer, ctx, flags);
            return result with { ComputeTimeMs = (float)sw.Elapsed.TotalMilliseconds };
        }
        finally
        {
            pool.Return(ctx);
        }
    }

    /// <summary>
    /// Versão que aceita GridPosition.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public PathResult FindPath(
        GridPosition start,
        GridPosition goal,
        ref GridPathBuffer pathBuffer,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        return FindPath(start.X, start.Y, goal.X, goal.Y, ref pathBuffer, flags);
    }

    private PathResult FindPathInternal(
        int startX, int startY,
        int goalX, int goalY,
        ref GridPathBuffer pathBuffer,
        PathfindingContext ctx,
        PathRequestFlags flags)
    {
        int startIndex = grid.CoordToIndex(startX, startY);
        int goalIndex = grid.CoordToIndex(goalX, goalY);
        int generation = ctx.Generation;

        bool cardinalOnly = flags.HasFlag(PathRequestFlags.CardinalOnly) || !_config.EnableDiagonalMovement;

        // Inicializa nó inicial
        ref PathNode startNode = ref ctx.Nodes[startIndex];
        startNode.X = startX;
        startNode.Y = startY;
        startNode.GCost = 0;
        startNode.HCost = Heuristic(startX, startY, goalX, goalY, cardinalOnly);
        startNode.ParentIndex = -1;
        startNode.Generation = generation;

        BinaryHeap.Push(ctx, startIndex);

        int nodesSearched = 0;
        int maxNodes = _config.MaxNodesPerSearch;
        int closestNodeIndex = startIndex;
        float closestDistance = startNode.HCost;

        while (!BinaryHeap.IsEmpty(ctx) && nodesSearched < maxNodes)
        {
            int currentIndex = BinaryHeap.Pop(ctx);
            ref PathNode current = ref ctx. Nodes[currentIndex];

            // Chegou ao objetivo?
            if (currentIndex == goalIndex)
            {
                return ReconstructPath(ctx, currentIndex, ref pathBuffer, generation, nodesSearched, goalX, goalY);
            }

            // Track nó mais próximo para partial path
            if (current.HCost < closestDistance)
            {
                closestDistance = current. HCost;
                closestNodeIndex = currentIndex;
            }

            ctx. MarkClosed(currentIndex);
            nodesSearched++;

            // Expande vizinhos
            ExpandNeighbors(ctx, ref current, currentIndex, goalX, goalY, generation, cardinalOnly);
        }

        // Não encontrou caminho completo
        if (flags.HasFlag(PathRequestFlags.AllowPartialPath) && closestNodeIndex != startIndex)
        {
            var (cx, cy) = grid.IndexToCoord(closestNodeIndex);
            return ReconstructPath(ctx, closestNodeIndex, ref pathBuffer, generation, nodesSearched, cx, cy);
        }

        return PathResult.Failed(
            nodesSearched >= maxNodes ? PathFailReason.Timeout : PathFailReason.NoPathExists,
            nodesSearched);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpandNeighbors(
        PathfindingContext ctx,
        ref PathNode current,
        int currentIndex,
        int goalX,
        int goalY,
        int generation,
        bool cardinalOnly)
    {
        int dirCount = cardinalOnly ? 4 : 8;
        int dirStep = cardinalOnly ? 2 : 1; // Pula diagonais se cardinal only

        for (int i = 0; i < 8; i += 1)
        {
            if (cardinalOnly && i % 2 == 1) continue; // Pula diagonais

            int nx = current.X + NavigationGrid.DirX[i];
            int ny = current.Y + NavigationGrid.DirY[i];

            if (!grid.IsWalkable(nx, ny))
                continue;

            // Corner cutting prevention para diagonais
            if (!cardinalOnly && i % 2 == 1 && _config.PreventCornerCutting)
            {
                int prevDir = (i + 7) % 8;
                int nextDir = (i + 1) % 8;
                bool blocked1 = !grid.IsWalkable(current.X + NavigationGrid.DirX[prevDir], current.Y + NavigationGrid.DirY[prevDir]);
                bool blocked2 = !grid.IsWalkable(current.X + NavigationGrid.DirX[nextDir], current.Y + NavigationGrid.DirY[nextDir]);
                if (blocked1 || blocked2)
                    continue;
            }

            int neighborIndex = grid.CoordToIndex(nx, ny);

            if (ctx.IsClosed(neighborIndex))
                continue;

            float moveCost = NavigationGrid.DirCost[i] * grid.GetMovementCost(nx, ny);
            float tentativeG = current.GCost + moveCost;

            ref PathNode neighbor = ref ctx.Nodes[neighborIndex];
            bool isNewNode = neighbor.Generation != generation;

            if (isNewNode || tentativeG < neighbor.GCost)
            {
                neighbor.X = nx;
                neighbor.Y = ny;
                neighbor.GCost = tentativeG;
                neighbor.HCost = Heuristic(nx, ny, goalX, goalY, cardinalOnly);
                neighbor.ParentIndex = currentIndex;
                neighbor.Generation = generation;

                if (isNewNode)
                {
                    BinaryHeap.Push(ctx, neighborIndex);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Heuristic(int x1, int y1, int x2, int y2, bool cardinalOnly)
    {
        int dx = Math.Abs(x1 - x2);
        int dy = Math.Abs(y1 - y2);

        if (cardinalOnly)
        {
            // Manhattan distance
            return dx + dy;
        }
        else
        {
            // Octile distance
            return Math.Max(dx, dy) + 0.414f * Math.Min(dx, dy);
        }
    }

    private PathResult ReconstructPath(
        PathfindingContext ctx,
        int goalIndex,
        ref GridPathBuffer pathBuffer,
        int generation,
        int nodesSearched,
        int goalX,
        int goalY)
    {
        // Conta nós do caminho (goal -> start)
        int length = 0;
        int current = goalIndex;

        while (current != -1 && length < ctx.TempPathCapacity)
        {
            ref PathNode node = ref ctx.Nodes[current];
            if (node.Generation != generation) break;

            ctx.TempPath[length++] = current;
            current = node.ParentIndex;
        }

        // Verifica se cabe no buffer
        if (length > GridPathBuffer.MaxWaypoints)
        {
            return PathResult.Failed(PathFailReason.BufferTooSmall, nodesSearched);
        }

        // Preenche buffer (invertendo para start -> goal)
        pathBuffer.Clear();
        pathBuffer.WaypointCount = length;
        pathBuffer.GoalX = goalX;
        pathBuffer.GoalY = goalY;

        for (int i = 0; i < length; i++)
        {
            // Inverte: primeiro waypoint é o mais próximo do start
            pathBuffer.SetWaypoint(i, ctx.TempPath[length - 1 - i]);
        }

        // Pula o primeiro waypoint (posição atual)
        if (pathBuffer.WaypointCount > 0)
        {
            pathBuffer.CurrentIndex = 1;
        }

        return PathResult.Succeeded(length, nodesSearched, 0);
    }
}