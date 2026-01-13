using System.Runtime.CompilerServices;
using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Services.Map;

/// <summary>
/// Mapa unificado de alta performance para MMORPG. 
/// 
/// Combina: 
/// - Tiles estáticos (colisão de terreno) em layout Morton
/// - Spatial hash de entidades (ocupação dinâmica) 
/// - Helpers de navegação
/// 
/// Otimizações:
/// - Morton Code para localidade de cache em queries espaciais
/// - Inline storage para células com poucas entidades
/// - Zero alocações em hot paths
/// - Ref returns para modificação in-place
/// </summary>
public sealed class WorldMap
{
    // --- Configuração ---
    public int Id { get; }
    public string Name { get; }
    public int Width { get; }
    public int Height { get; }
    public int Layers { get; }
    public MapFlags Flags { get; set; }
    public string? BgmId { get; set; }
    
    // Dimensão física (potência de 2 para Morton)
    private readonly int _physicalSide;
    private readonly int _physicalArea;
    
    // --- Storage ---
    // Tiles por camada (Morton order)
    private readonly Tile[][] _tiles;
    
    // Entidades por célula (mesmo índice Morton que tiles)
    // Cada layer tem seu próprio spatial
    private readonly CellOccupants[][] _occupants;
    
    // Pool de listas para overflow
    private readonly Stack<List<Entity>> _listPool = new(64);
    
    // --- Direções para navegação ---
    private static readonly (int Dx, int Dy, float Cost)[] CardinalDirs =
    [
        (0, -1, 1.0f),  // N
        (1, 0, 1.0f),   // E
        (0, 1, 1.0f),   // S
        (-1, 0, 1.0f)   // W
    ];
    
    private static readonly (int Dx, int Dy, float Cost)[] AllDirs =
    [
        (0, -1, 1.0f),    // N
        (1, -1, 1.414f),  // NE
        (1, 0, 1.0f),     // E
        (1, 1, 1.414f),   // SE
        (0, 1, 1.0f),     // S
        (-1, 1, 1.414f),  // SW
        (-1, 0, 1.0f),    // W
        (-1, -1, 1.414f)  // NW
    ];
    
    public WorldMap(int id, string name, int width, int height, int layers = 1, MapFlags flags = MapFlags.None, string? bgmId = null)
    {
        if (width <= 0 || height <= 0 || layers <= 0)
            throw new ArgumentException("Dimensions must be positive");
        
        Id = id;
        Name = name ??  $"Map_{id}";
        Width = width;
        Height = height;
        Layers = layers;
        Flags = flags;
        BgmId = bgmId;
        
        _physicalSide = Morton.NextPow2(Math.Max(width, height));
        _physicalArea = _physicalSide * _physicalSide;
        
        // Validação de tamanho (limite do array C#)
        if ((long)_physicalArea * layers > Array.MaxLength)
            throw new OverflowException($"Map too large: {width}x{height}x{layers}");
        
        _tiles = new Tile[layers][];
        _occupants = new CellOccupants[layers][];
        
        for (int z = 0; z < layers; z++)
        {
            _tiles[z] = new Tile[_physicalArea];
            _occupants[z] = new CellOccupants[_physicalArea];
            
            // Inicializa tiles como vazios (walkable)
            Array.Fill(_tiles[z], Tile. Empty);
        }
    }
    
    // ========== INDEXAÇÃO ==========
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public bool InBounds(int x, int y, int z = 0)
        => (uint)x < (uint)Width && (uint)y < (uint)Height && (uint)z < (uint)Layers;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InBounds(Position pos) => InBounds(pos.X, pos.Y, pos. Z);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetIndex(int x, int y) => (int)Morton.Encode(x, y);
    
    // ========== TILES (TERRENO ESTÁTICO) ==========
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public ref Tile GetTileRef(int x, int y, int z = 0)
    {
        if (! InBounds(x, y, z))
            throw new ArgumentOutOfRangeException();
        return ref _tiles[z][GetIndex(x, y)];
    }
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public Tile GetTile(int x, int y, int z = 0)
    {
        if (!InBounds(x, y, z)) return Tile. Blocked;
        return _tiles[z][GetIndex(x, y)];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Tile GetTile(Position pos) => GetTile(pos. X, pos.Y, pos.Z);
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public void SetTile(int x, int y, int z, Tile tile)
    {
        if (!InBounds(x, y, z)) return;
        _tiles[z][GetIndex(x, y)] = tile;
    }
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public void SetTile(Position pos, Tile tile) => SetTile(pos.X, pos.Y, pos.Z, tile);
    
    public void FillLayer(int z, Tile tile)
    {
        if ((uint)z >= (uint)Layers) return;
        Array.Fill(_tiles[z], tile);
    }
    
    // ========== COLISÃO (TERRENO) ==========
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBlocked(int x, int y, int z = 0)
    {
        if (! InBounds(x, y, z)) return true;
        return _tiles[z][GetIndex(x, y)].IsBlocked;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBlocked(Position pos) => IsBlocked(pos.X, pos.Y, pos.Z);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetMovementCost(int x, int y, int z = 0)
    {
        if (!InBounds(x, y, z)) return float.MaxValue;
        return _tiles[z][GetIndex(x, y)].Cost;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetMovementCost(Position pos) => GetMovementCost(pos.X, pos.Y, pos.Z);
    
    public bool IsAreaBlocked(Position min, Position max)
    {
        int minX = Math. Max(0, min.X), maxX = Math. Min(Width - 1, max.X);
        int minY = Math.Max(0, min. Y), maxY = Math.Min(Height - 1, max.Y);
        int z = Math. Clamp(min.Z, 0, Layers - 1);
        
        var layer = _tiles[z];
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (layer[GetIndex(x, y)].IsBlocked)
                    return true;
            }
        }
        return false;
    }
    
    // ========== ENTIDADES (OCUPAÇÃO DINÂMICA) ==========
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(int x, int y, int z = 0)
    {
        if (!InBounds(x, y)) return false;
        return _occupants[z][GetIndex(x, y)].Count > 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(Position pos) => IsOccupied(pos.X, pos. Y, pos.Z);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkableAndFree(int x, int y, int z = 0)
        => ! IsBlocked(x, y, z) && !IsOccupied(x, y, z);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkableAndFree(Position pos) 
        => IsWalkableAndFree(pos.X, pos.Y, pos.Z);
    
    public void AddEntity(Position pos, Entity entity)
    {
        if (!InBounds(pos)) return;
        ref var cell = ref _occupants[pos.Z][GetIndex(pos.X, pos.Y)];
        cell.Add(entity, _listPool);
    }
    
    public bool RemoveEntity(Position pos, Entity entity)
    {
        if (!InBounds(pos)) return false;
        ref var cell = ref _occupants[pos.Z][GetIndex(pos. X, pos.Y)];
        return cell.Remove(entity, _listPool);
    }
    
    public bool MoveEntity(Position from, Position to, Entity entity)
    {
        if (!InBounds(from) || !InBounds(to)) return false;
        
        ref var oldCell = ref _occupants[from.Z][GetIndex(from.X, from. Y)];
        if (! oldCell.Remove(entity, _listPool)) return false;
        
        ref var newCell = ref _occupants[to.Z][GetIndex(to.X, to. Y)];
        newCell.Add(entity, _listPool);
        return true;
    }
    
    /// <summary>
    /// Move apenas se destino estiver livre. 
    /// </summary>
    public bool TryMoveEntity(Position from, Position to, Entity entity)
    {
        if (IsOccupied(to)) return false;
        return MoveEntity(from, to, entity);
    }
    
    public bool TryGetFirstEntity(Position pos, out Entity entity)
    {
        entity = Entity. Null;
        if (!InBounds(pos)) return false;
        
        ref var cell = ref _occupants[pos. Z][GetIndex(pos.X, pos.Y)];
        if (cell. Count == 0) return false;
        
        entity = cell.GetFirst();
        return true;
    }
    
    public int GetEntitiesAt(Position pos, Span<Entity> buffer)
    {
        if (!InBounds(pos)) return 0;
        ref var cell = ref _occupants[pos. Z][GetIndex(pos.X, pos.Y)];
        return cell.CopyTo(buffer);
    }
    
    /// <summary>
    /// Query de área retangular.
    /// </summary>
    public int QueryEntities(Position min, Position max, Span<Entity> buffer)
    {
        int count = 0;
        int z = Math. Clamp(min. Z, 0, Layers - 1);
        var layer = _occupants[z];
        
        int minX = Math. Max(0, min.X), maxX = Math.Min(Width - 1, max.X);
        int minY = Math.Max(0, min.Y), maxY = Math. Min(Height - 1, max.Y);
        
        for (int y = minY; y <= maxY && count < buffer.Length; y++)
        {
            for (int x = minX; x <= maxX && count < buffer. Length; x++)
            {
                ref var cell = ref layer[GetIndex(x, y)];
                count += cell.CopyTo(buffer[count..]);
            }
        }
        
        return count;
    }
    
    /// <summary>
    /// Query circular (ideal para AOI de NPCs/Players).
    /// </summary>
    public int QueryEntitiesCircle(Position center, int radius, Span<Entity> buffer)
    {
        int count = 0;
        int z = Math.Clamp(center.Z, 0, Layers - 1);
        var layer = _occupants[z];
        int radiusSq = radius * radius;
        
        for (int dy = -radius; dy <= radius && count < buffer.Length; dy++)
        {
            int dySq = dy * dy;
            int maxDx = (int)Math.Sqrt(radiusSq - dySq);
            
            for (int dx = -maxDx; dx <= maxDx && count < buffer. Length; dx++)
            {
                int x = center.X + dx;
                int y = center.Y + dy;
                
                if (! InBounds(x, y, z)) continue;
                
                ref var cell = ref layer[GetIndex(x, y)];
                count += cell.CopyTo(buffer[count.. ]);
            }
        }
        
        return count;
    }
    
    // ========== NAVEGAÇÃO ==========
    
    /// <summary>
    /// Retorna vizinhos walkable (para pathfinding).
    /// </summary>
    public int GetWalkableNeighbors(Position pos, Span<Position> neighbors, bool diagonal = false)
    {
        var dirs = diagonal ? AllDirs : CardinalDirs;
        int count = 0;
        
        foreach (var (dx, dy, _) in dirs)
        {
            int nx = pos.X + dx, ny = pos.Y + dy;
            
            if (! InBounds(nx, ny, pos. Z)) continue;
            if (IsBlocked(nx, ny, pos.Z)) continue;
            
            // Evita cortar cantos em diagonais
            if (dx != 0 && dy != 0)
            {
                if (IsBlocked(pos.X + dx, pos.Y, pos.Z) || 
                    IsBlocked(pos. X, pos.Y + dy, pos. Z))
                    continue;
            }
            
            if (count < neighbors.Length)
                neighbors[count++] = new Position(nx, ny, pos.Z);
        }
        
        return count;
    }
    
    /// <summary>
    /// Retorna vizinhos walkable E não ocupados. 
    /// </summary>
    public int GetFreeNeighbors(Position pos, Span<Position> neighbors, bool diagonal = false)
    {
        var dirs = diagonal ? AllDirs : CardinalDirs;
        int count = 0;
        
        foreach (var (dx, dy, _) in dirs)
        {
            int nx = pos.X + dx, ny = pos.Y + dy;
            
            if (!IsWalkableAndFree(nx, ny, pos.Z)) continue;
            
            // Evita cortar cantos
            if (dx != 0 && dy != 0)
            {
                if (IsBlocked(pos.X + dx, pos.Y, pos.Z) || 
                    IsBlocked(pos. X, pos.Y + dy, pos. Z))
                    continue;
            }
            
            if (count < neighbors. Length)
                neighbors[count++] = new Position(nx, ny, pos.Z);
        }
        
        return count;
    }
    
    // ========== VALIDAÇÃO DE MOVIMENTO ==========
    
    public MoveResult ValidateMove(Position target, Entity movingEntity)
    {
        if (!InBounds(target))
            return MoveResult.OutOfBounds;
        
        if (IsBlocked(target))
            return MoveResult.BlockedByTerrain;
        
        if (TryGetFirstEntity(target, out var occupant) && 
            occupant != Entity.Null && 
            occupant != movingEntity)
        {
            return MoveResult.BlockedByEntity;
        }
        
        return MoveResult.Success;
    }
    
    // ========== BULK OPERATIONS ==========
    
    /// <summary>
    /// Carrega tiles de um array linear (row-major, ex: de arquivo).
    /// </summary>
    public void LoadTiles(int z, ReadOnlySpan<Tile> linearTiles)
    {
        if ((uint)z >= (uint)Layers) return;
        if (linearTiles. Length != Width * Height)
            throw new ArgumentException("Tile count mismatch");
        
        var layer = _tiles[z];
        
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int linearIdx = y * Width + x;
                int mortonIdx = GetIndex(x, y);
                layer[mortonIdx] = linearTiles[linearIdx];
            }
        }
    }
    
    /// <summary>
    /// Exporta tiles para array linear (para salvar/enviar).
    /// </summary>
    public void ExportTiles(int z, Span<Tile> linearTiles)
    {
        if ((uint)z >= (uint)Layers) return;
        if (linearTiles.Length < Width * Height)
            throw new ArgumentException("Buffer too small");
        
        var layer = _tiles[z];
        
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int linearIdx = y * Width + x;
                int mortonIdx = GetIndex(x, y);
                linearTiles[linearIdx] = layer[mortonIdx];
            }
        }
    }
    
    /// <summary>
    /// Copia região para buffer (enviar chunk para cliente).
    /// </summary>
    public void CopyRegion(Position start, int width, int height, int z, Span<Tile> buffer)
    {
        if ((uint)z >= (uint)Layers) return;
        
        var layer = _tiles[z];
        int idx = 0;
        
        for (int dy = 0; dy < height && idx < buffer.Length; dy++)
        {
            for (int dx = 0; dx < width && idx < buffer.Length; dx++)
            {
                int x = start.X + dx, y = start.Y + dy;
                buffer[idx++] = InBounds(x, y, z) ? layer[GetIndex(x, y)] : Tile.Blocked;
            }
        }
    }
    
    // ========== STATS ==========
    
    public (int TotalCells, int OccupiedCells, int TotalEntities) GetStats()
    {
        int occupied = 0, total = 0;
        
        foreach (var layer in _occupants)
        {
            foreach (ref var cell in layer. AsSpan())
            {
                if (cell.Count > 0)
                {
                    occupied++;
                    total += cell.Count;
                }
            }
        }
        
        return (Width * Height * Layers, occupied, total);
    }
}

public enum MoveResult :  byte
{
    Success,
    OutOfBounds,
    BlockedByTerrain,
    BlockedByEntity
}

[Flags]
public enum MapFlags :  byte
{
    None = 0,
    PvPEnabled = 1 << 0,
    NoTeleport = 1 << 1,
    NoRecall = 1 << 2,
    Indoors = 1 << 3,
    NightEnabled = 1 << 4,
    WeatherEnabled = 1 << 5
}