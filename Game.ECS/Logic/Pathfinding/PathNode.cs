using Game.ECS.Components;

namespace Game.ECS.Logic.Pathfinding;

/// <summary>
/// Nó usado pelo algoritmo A* para representar uma célula no grid.
/// Struct para evitar alocações no heap.
/// </summary>
public struct PathNode
{
    /// <summary>Posição deste nó no grid</summary>
    public Position Position;
    
    /// <summary>Custo do caminho desde o início até este nó (G-cost)</summary>
    public int GCost;
    
    /// <summary>Heurística estimada deste nó até o destino (H-cost)</summary>
    public int HCost;
    
    /// <summary>Posição do nó pai para reconstrução do caminho</summary>
    public Position Parent;
    
    /// <summary>Se este nó foi fechado (já processado)</summary>
    public bool IsClosed;
    
    /// <summary>Custo total (F = G + H)</summary>
    public readonly int FCost => GCost + HCost;
    
    public PathNode(Position position, int gCost, int hCost, Position parent)
    {
        Position = position;
        GCost = gCost;
        HCost = hCost;
        Parent = parent;
        IsClosed = false;
    }
    
    /// <summary>
    /// Calcula a distância Manhattan entre duas posições.
    /// Usado como heurística admissível para A* em grids 4-direcionais.
    /// </summary>
    public static int ManhattanDistance(Position a, Position b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
    }
    
    /// <summary>
    /// Calcula a distância Chebyshev (diagonal) entre duas posições.
    /// Usado como heurística para A* em grids 8-direcionais.
    /// </summary>
    public static int ChebyshevDistance(Position a, Position b)
    {
        int dx = Math.Abs(a.X - b.X);
        int dy = Math.Abs(a.Y - b.Y);
        return Math.Max(dx, dy);
    }
}

/// <summary>
/// Resultado do pathfinding A*.
/// </summary>
public enum PathfindingResult : byte
{
    /// <summary>Caminho encontrado com sucesso</summary>
    Success = 0,
    
    /// <summary>Destino é o mesmo que origem ou muito próximo</summary>
    AlreadyAtDestination = 1,
    
    /// <summary>Destino está bloqueado</summary>
    DestinationBlocked = 2,
    
    /// <summary>Não foi possível encontrar caminho (sem rota)</summary>
    NoPath = 3,
    
    /// <summary>Limite de nós expandidos atingido</summary>
    MaxNodesReached = 4,
    
    /// <summary>Origem está fora dos limites do mapa</summary>
    OutOfBounds = 5
}
