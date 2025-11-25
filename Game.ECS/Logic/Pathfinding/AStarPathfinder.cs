using System.Buffers;
using System.Runtime.CompilerServices;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Logic.Pathfinding;

/// <summary>
/// Implementação otimizada do algoritmo A* para pathfinding em grid tile-based.
/// 
/// Otimizações implementadas:
/// - ArrayPool para evitar alocações
/// - Heurística Manhattan para grids 4-direcionais
/// - Early exit conditions
/// - Limite de nós expandidos para evitar travamentos
/// </summary>
public sealed class AStarPathfinder
{
    /// <summary>Número máximo de nós que podem ser expandidos antes de abortar</summary>
    public const int MaxExpandedNodes = 500;
    
    /// <summary>Custo de movimento cardinal (horizontal/vertical)</summary>
    private const int CardinalCost = 10;
    
    /// <summary>Custo de movimento diagonal (aproximação de √2 * 10)</summary>
    private const int DiagonalCost = 14;
    
    // Direções cardinais (4 direções)
    private static readonly (int dx, int dy)[] CardinalDirections =
    [
        (0, -1),  // Norte
        (1, 0),   // Leste
        (0, 1),   // Sul
        (-1, 0)   // Oeste
    ];
    
    // Direções 8-way (inclui diagonais)
    private static readonly (int dx, int dy)[] AllDirections =
    [
        (0, -1),   // Norte
        (1, -1),   // Nordeste
        (1, 0),    // Leste
        (1, 1),    // Sudeste
        (0, 1),    // Sul
        (-1, 1),   // Sudoeste
        (-1, 0),   // Oeste
        (-1, -1)   // Noroeste
    ];
    
    /// <summary>
    /// Calcula o caminho mais curto entre duas posições usando A*.
    /// </summary>
    /// <param name="grid">Grid do mapa para verificar bloqueios</param>
    /// <param name="start">Posição inicial</param>
    /// <param name="goal">Posição destino</param>
    /// <param name="floor">Nível/andar do mapa</param>
    /// <param name="path">Buffer para armazenar o caminho (waypoints)</param>
    /// <param name="pathLength">Quantidade de waypoints escritos no buffer</param>
    /// <param name="allowDiagonal">Se permite movimento diagonal</param>
    /// <returns>Resultado do pathfinding</returns>
    public static PathfindingResult FindPath(
        IMapGrid grid,
        Position start,
        Position goal,
        sbyte floor,
        Span<Position> path,
        out int pathLength,
        bool allowDiagonal = false)
    {
        pathLength = 0;
        
        // Early exit: mesma posição
        if (start == goal)
            return PathfindingResult.AlreadyAtDestination;
        
        // Early exit: muito próximo (1 tile)
        int distSq = (goal.X - start.X) * (goal.X - start.X) + (goal.Y - start.Y) * (goal.Y - start.Y);
        if (distSq <= 2)
        {
            // Apenas 1 waypoint direto se adjacente
            if (!grid.IsBlocked(new SpatialPosition(goal.X, goal.Y, floor)))
            {
                path[0] = goal;
                pathLength = 1;
                return PathfindingResult.Success;
            }
        }
        
        // Early exit: destino bloqueado
        if (grid.IsBlocked(new SpatialPosition(goal.X, goal.Y, floor)))
            return PathfindingResult.DestinationBlocked;
        
        // Early exit: origem fora dos limites
        if (!grid.InBounds(new SpatialPosition(start.X, start.Y, floor)))
            return PathfindingResult.OutOfBounds;
        
        var directions = allowDiagonal ? AllDirections : CardinalDirections;
        
        // Estruturas para o A*
        // Usamos um dictionary para armazenar nós (position -> node)
        var nodes = new Dictionary<Position, PathNode>(MaxExpandedNodes);
        
        // Open list como min-heap usando PriorityQueue
        var openList = new PriorityQueue<Position, int>(MaxExpandedNodes);
        
        // Inicializa nó inicial
        int startH = PathNode.ManhattanDistance(start, goal) * CardinalCost;
        var startNode = new PathNode(start, 0, startH, start);
        nodes[start] = startNode;
        openList.Enqueue(start, startNode.FCost);
        
        int expandedCount = 0;
        
        while (openList.Count > 0 && expandedCount < MaxExpandedNodes)
        {
            // Pega o nó com menor F-cost
            var currentPos = openList.Dequeue();
            
            // Pula se já foi fechado (pode ter duplicatas na queue)
            if (!nodes.TryGetValue(currentPos, out var currentNode) || currentNode.IsClosed)
                continue;
            
            // Marca como fechado
            currentNode.IsClosed = true;
            nodes[currentPos] = currentNode;
            expandedCount++;
            
            // Chegou ao destino!
            if (currentPos == goal)
            {
                return ReconstructPath(nodes, goal, path, out pathLength);
            }
            
            // Expande vizinhos
            for (int i = 0; i < directions.Length; i++)
            {
                var (dx, dy) = directions[i];
                var neighborPos = new Position(currentPos.X + dx, currentPos.Y + dy);
                var neighborSpatial = new SpatialPosition(neighborPos.X, neighborPos.Y, floor);
                
                // Verifica se está nos limites e não está bloqueado
                if (!grid.InBounds(neighborSpatial) || grid.IsBlocked(neighborSpatial))
                    continue;
                
                // Se diagonal, verifica se os tiles adjacentes estão livres (evita cortar cantos)
                if (allowDiagonal && dx != 0 && dy != 0)
                {
                    if (grid.IsBlocked(new SpatialPosition(currentPos.X + dx, currentPos.Y, floor)) ||
                        grid.IsBlocked(new SpatialPosition(currentPos.X, currentPos.Y + dy, floor)))
                        continue;
                }
                
                // Calcula custo
                int moveCost = (dx != 0 && dy != 0) ? DiagonalCost : CardinalCost;
                int tentativeG = currentNode.GCost + moveCost;
                
                // Verifica se já temos um caminho melhor para este nó
                if (nodes.TryGetValue(neighborPos, out var existingNode))
                {
                    if (existingNode.IsClosed || tentativeG >= existingNode.GCost)
                        continue;
                }
                
                // Atualiza ou cria nó
                int h = PathNode.ManhattanDistance(neighborPos, goal) * CardinalCost;
                var newNode = new PathNode(neighborPos, tentativeG, h, currentPos);
                nodes[neighborPos] = newNode;
                
                // Adiciona à open list
                openList.Enqueue(neighborPos, newNode.FCost);
            }
        }
        
        // Não encontrou caminho
        return expandedCount >= MaxExpandedNodes 
            ? PathfindingResult.MaxNodesReached 
            : PathfindingResult.NoPath;
    }
    
    /// <summary>
    /// Reconstrói o caminho a partir dos nós, escrevendo waypoints no buffer.
    /// O caminho é retornado na ordem correta (do início ao destino).
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static PathfindingResult ReconstructPath(
        Dictionary<Position, PathNode> nodes,
        Position goal,
        Span<Position> path,
        out int pathLength)
    {
        pathLength = 0;
        
        // Primeiro, conta quantos nós há no caminho
        var tempPath = ArrayPool<Position>.Shared.Rent(NpcPath.MaxWaypoints);
        try
        {
            int count = 0;
            var current = goal;
            
            while (nodes.TryGetValue(current, out var node) && count < NpcPath.MaxWaypoints)
            {
                tempPath[count++] = current;
                
                // Se chegou ao início (parent é ele mesmo)
                if (node.Parent == current)
                    break;
                
                current = node.Parent;
            }
            
            // Inverte o caminho (estava do fim pro início)
            // Pula o primeiro waypoint (posição atual) para começar do próximo
            int writeIndex = 0;
            for (int i = count - 2; i >= 0 && writeIndex < path.Length; i--)
            {
                path[writeIndex++] = tempPath[i];
            }
            
            pathLength = writeIndex;
            return PathfindingResult.Success;
        }
        finally
        {
            ArrayPool<Position>.Shared.Return(tempPath);
        }
    }
    
    /// <summary>
    /// Versão simplificada que escreve diretamente em um NpcPath.
    /// </summary>
    public static PathfindingResult FindPath(
        IMapGrid grid,
        Position start,
        Position goal,
        sbyte floor,
        ref NpcPath npcPath,
        bool allowDiagonal = false)
    {
        // Buffer temporário para o caminho
        Span<Position> tempPath = stackalloc Position[NpcPath.MaxWaypoints];
        
        var result = FindPath(grid, start, goal, floor, tempPath, out int pathLength, allowDiagonal);
        
        if (result == PathfindingResult.Success)
        {
            // Copia para o NpcPath
            npcPath.ClearPath();
            for (int i = 0; i < pathLength && i < NpcPath.MaxWaypoints; i++)
            {
                npcPath.SetWaypoint(i, tempPath[i]);
            }
            npcPath.WaypointCount = (byte)Math.Min(pathLength, NpcPath.MaxWaypoints);
            npcPath.CurrentIndex = 0;
            npcPath.NeedsRecalculation = false;
            npcPath.LastTargetPosition = goal;
        }
        
        return result;
    }
}
