using System.Runtime.CompilerServices;
using Game.ECS.Navigation.Components;
using Game.ECS.Navigation.Core;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Pathfinding A* otimizado que usa IMapData.
/// </summary>
public sealed class PathfindingSystem(
    PathfindingPool pool, 
    IMapData mapData, 
    int mapWidth, 
    int mapHeight)
{
    private static readonly int[] DirX = [0, 1, 1, 1, 0, -1, -1, -1];
    private static readonly int[] DirY = [-1, -1, 0, 1, 1, 1, 0, -1];
    private static readonly float[] DirCost = [1f, 1.414f, 1f, 1.414f, 1f, 1.414f, 1f, 1.414f];

    public PathResult FindPath(ref PathfindingRequest request, Span<int> pathBuffer)
    {
        var ctx = pool.RentContext();
        try
        {
            return FindPathInternal(ref request, pathBuffer, ctx);
        }
        finally
        {
            pool.ReturnContext(ctx);
        }
    }

    private PathResult FindPathInternal(
        ref PathfindingRequest request,
        Span<int> pathBuffer,
        PathfindingContext ctx)
    {
        int startIndex = CoordToIndex(request.StartX, request.StartY);
        int goalIndex = CoordToIndex(request.GoalX, request.GoalY);
        int generation = ctx.Generation;

        // Validação rápida
        if (!IsValidCoord(request.StartX, request.StartY) ||
            !IsValidCoord(request.GoalX, request.GoalY) ||
            !IsWalkable(request.StartX, request.StartY) || 
            !IsWalkable(request.GoalX, request.GoalY))
        {
            request.Status = PathfindingStatus.Failed;
            return new PathResult { IsValid = false };
        }

        // Inicializa nó inicial
        ref PathNode startNode = ref ctx.Nodes[startIndex];
        startNode.X = request.StartX;
        startNode.Y = request.StartY;
        startNode.GCost = 0;
        startNode.HCost = Heuristic(request.StartX, request.StartY, request.GoalX, request.GoalY);
        startNode.ParentIndex = -1;
        startNode.Generation = generation;

        BinaryHeap.Push(ctx, startIndex, ctx.Nodes);

        int nodesSearched = 0;
        int maxNodes = request.MaxSearchNodes > 0 
            ? Math.Min(request.MaxSearchNodes, ctx.NodeCapacity)
            : ctx.NodeCapacity;

        while (ctx.OpenCount > 0 && nodesSearched < maxNodes)
        {
            int currentIndex = BinaryHeap.Pop(ctx, ctx.Nodes);
            ref PathNode current = ref ctx.Nodes[currentIndex];

            // Chegou ao objetivo?
            if (currentIndex == goalIndex)
            {
                request.Status = PathfindingStatus.Completed;
                return ReconstructPath(ctx, currentIndex, pathBuffer, generation);
            }

            ctx.MarkClosed(currentIndex);
            nodesSearched++;

            // Expande vizinhos
            ExpandNeighbors(ctx, ref current, currentIndex, request.GoalX, request.GoalY, generation);
        }

        request.Status = PathfindingStatus.Failed;
        return new PathResult { IsValid = false };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ExpandNeighbors(
        PathfindingContext ctx,
        ref PathNode current,
        int currentIndex,
        int goalX,
        int goalY,
        int generation)
    {
        for (int i = 0; i < 8; i++)
        {
            int nx = current.X + DirX[i];
            int ny = current.Y + DirY[i];

            if (!IsValidCoord(nx, ny) || !IsWalkable(nx, ny))
                continue;

            // Verifica diagonal bloqueada (corner cutting)
            if (i % 2 == 1)
            {
                int cardinalX = current.X + DirX[(i + 7) % 8];
                int cardinalY = current.Y + DirY[(i + 1) % 8];
                if (!IsWalkable(cardinalX, current.Y) || !IsWalkable(current.X, cardinalY))
                    continue;
            }

            int neighborIndex = CoordToIndex(nx, ny);

            if (ctx.IsClosed(neighborIndex))
                continue;

            float tentativeG = current.GCost + DirCost[i] * mapData.GetMovementCost(nx, ny);
            ref PathNode neighbor = ref ctx.Nodes[neighborIndex];

            bool isNewNode = neighbor.Generation != generation;
            
            if (isNewNode || tentativeG < neighbor.GCost)
            {
                neighbor.X = nx;
                neighbor.Y = ny;
                neighbor.GCost = tentativeG;
                neighbor.HCost = Heuristic(nx, ny, goalX, goalY);
                neighbor.ParentIndex = currentIndex;
                neighbor.Generation = generation;

                if (isNewNode)
                {
                    BinaryHeap.Push(ctx, neighborIndex, ctx.Nodes);
                }
            }
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static float Heuristic(int x1, int y1, int x2, int y2)
    {
        int dx = Math.Abs(x1 - x2);
        int dy = Math.Abs(y1 - y2);
        return Math.Max(dx, dy) + 0.414f * Math.Min(dx, dy);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int CoordToIndex(int x, int y) => y * mapWidth + x;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsValidCoord(int x, int y) => x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool IsWalkable(int x, int y) => mapData.IsWalkable(x, y);

    private PathResult ReconstructPath(PathfindingContext ctx, int goalIndex, Span<int> pathBuffer, int generation)
    {
        int length = 0;
        int current = goalIndex;

        while (current != -1 && length < pathBuffer.Length)
        {
            ref PathNode node = ref ctx.Nodes[current];
            if (node.Generation != generation) break;
            
            pathBuffer[length++] = current;
            current = node.ParentIndex;
        }

        pathBuffer[..length].Reverse();

        return new PathResult
        {
            PathLength = length,
            IsValid = true
        };
    }
}

public interface IMapData
{
    bool IsWalkable(int x, int y);
    float GetMovementCost(int x, int y);
}