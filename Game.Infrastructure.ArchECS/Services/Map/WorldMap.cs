using System.Runtime.CompilerServices;
using Arch.Core;
using Game.Infrastructure.ArchECS.Commons.Components;

namespace Game.Infrastructure.ArchECS.Services.Map;

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
    public int Floors { get; }
    public MapFlags Flags { get; set; }
    public string? BgmId { get; set; }
    
    public int DefaultSpawnX { get; set; }
    public int DefaultSpawnY { get; set; }
    public int DefaultFloor { get; set; }
    
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
    
    public WorldMap(int id, string name, int width, int height, int floors = 1, MapFlags flags = MapFlags.None, string? bgmId = null,
        int defaultSpawnX = 0, int defaultSpawnY = 0, int defaultFloor = 0)
    {
        if (width <= 0 || height <= 0 || floors <= 0)
            throw new ArgumentException("Dimensions must be positive");
        
        Id = id;
        Name = name ??  $"Map_{id}";
        Width = width;
        Height = height;
        Floors = floors;
        Flags = flags;
        BgmId = bgmId;
        DefaultSpawnX = defaultSpawnX;
        DefaultSpawnY = defaultSpawnY;
        DefaultFloor = defaultFloor;
        
        var physicalSide = Morton.NextPow2(Math.Max(width, height));
        var physicalArea = physicalSide * physicalSide;

        // Validação de tamanho (limite do array C#)
        if ((long)physicalArea * floors > Array.MaxLength)
            throw new OverflowException($"Map too large: {width}x{height}x{floors}");
        
        _tiles = new Tile[floors][];
        _occupants = new CellOccupants[floors][];
        
        for (int floor = 0; floor < floors; floor++)
        {
            _tiles[floor] = new Tile[physicalArea];
            _occupants[floor] = new CellOccupants[physicalArea];
            
            // Inicializa tiles como vazios (walkable)
            Array.Fill(_tiles[floor], Tile. Empty);
        }
    }
    
    // ========== INDEXAÇÃO ==========
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public bool InBounds(int x, int y, int floor)
        => (uint)x < (uint)Width && (uint)y < (uint)Height && (uint)floor < (uint)Floors;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool InBounds(Position pos, int floor) => InBounds(pos.X, pos.Y, floor);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int GetIndex(int x, int y) => (int)Morton.Encode(x, y);
    
    // ========== TILES (TERRENO ESTÁTICO) ==========
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public ref Tile GetTileRef(int x, int y, int floor = 0)
    {
        if (! InBounds(x, y, floor))
            throw new ArgumentOutOfRangeException();
        return ref _tiles[floor][GetIndex(x, y)];
    }
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public Tile GetTile(int x, int y, int floor = 0)
    {
        if (!InBounds(x, y, floor)) return Tile. Blocked;
        return _tiles[floor][GetIndex(x, y)];
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Tile GetTile(Position pos, int floor) => GetTile(pos. X, pos.Y, floor);
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public void SetTile(int x, int y, int z, Tile tile)
    {
        if (!InBounds(x, y, z)) return;
        _tiles[z][GetIndex(x, y)] = tile;
    }
    
    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    public void SetTile(Position pos, int floor, Tile tile) => SetTile(pos.X, pos.Y, floor, tile);
    
    public void FillLayer(int floor, Tile tile)
    {
        if ((uint)floor >= (uint)Floors) return;
        Array.Fill(_tiles[floor], tile);
    }
    
    // ========== COLISÃO (TERRENO) ==========
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBlocked(int x, int y, int floor = 0)
    {
        if (! InBounds(x, y, floor)) return true;
        return _tiles[floor][GetIndex(x, y)].IsBlocked;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsBlocked(Position pos, int floor) => IsBlocked(pos.X, pos.Y, floor);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetMovementCost(int x, int y, int floor = 0)
    {
        if (!InBounds(x, y, floor)) return float.MaxValue;
        return _tiles[floor][GetIndex(x, y)].Cost;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public float GetMovementCost(Position pos, int floor) => GetMovementCost(pos.X, pos.Y, floor);
    
    public bool IsAreaBlocked(Position min, Position max, int floor)
    {
        int minX = Math. Max(0, min.X), maxX = Math. Min(Width - 1, max.X);
        int minY = Math.Max(0, min. Y), maxY = Math.Min(Height - 1, max.Y);
        int z = Math.Clamp(floor, 0, Floors - 1);
        
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
    public bool IsOccupied(int x, int y, int floor)
    {
        if (!InBounds(x, y, floor)) return false;
        return _occupants[floor][GetIndex(x, y)].Count > 0;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsOccupied(Position pos, int floor) => IsOccupied(pos.X, pos. Y, floor);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkableAndFree(int x, int y, int z = 0)
        => ! IsBlocked(x, y, z) && !IsOccupied(x, y, z);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsWalkableAndFree(Position pos, int floor) 
        => IsWalkableAndFree(pos.X, pos.Y, floor);
    
    public bool AddEntity(Position pos, int floor, Entity entity)
    {
        if (!InBounds(pos, floor)) return false;
        ref var cell = ref _occupants[floor][GetIndex(pos.X, pos.Y)];
        cell.Add(entity, _listPool);
        return true;
    }
    
    public bool RemoveEntity(Position pos, int floor, Entity entity)
    {
        if (!InBounds(pos, floor)) return false;
        ref var cell = ref _occupants[floor][GetIndex(pos. X, pos.Y)];
        return cell.Remove(entity, _listPool);
    }
    
    public bool MoveEntity(Position from, int fromFloor, Position to, int toFloor, Entity entity)
    {
        if (!InBounds(from, fromFloor) || !InBounds(to, toFloor)) return false;
        
        ref var oldCell = ref _occupants[fromFloor][GetIndex(from.X, from. Y)];
        if (! oldCell.Remove(entity, _listPool)) return false;
        
        ref var newCell = ref _occupants[toFloor][GetIndex(to.X, to. Y)];
        newCell.Add(entity, _listPool);
        return true;
    }
    
    /// <summary>
    /// Move apenas se destino estiver livre. 
    /// </summary>
    public bool TryMoveEntity(Position from, int fromFloor, Position to, int toFloor, Entity entity)
    {
        if (IsOccupied(to, toFloor)) return false;
        return MoveEntity(from, fromFloor, to, toFloor, entity);
    }
    
    public bool TryGetFirstEntity(Position pos, int floor, out Entity entity)
    {
        entity = Entity. Null;
        if (!InBounds(pos, floor)) return false;
        
        ref var cell = ref _occupants[floor][GetIndex(pos.X, pos.Y)];
        if (cell. Count == 0) return false;
        
        entity = cell.GetFirst();
        return true;
    }
    
    public int GetEntitiesAt(Position pos, int floor, Span<Entity> buffer)
    {
        if (!InBounds(pos, floor)) return 0;
        ref var cell = ref _occupants[floor][GetIndex(pos.X, pos.Y)];
        return cell.CopyTo(buffer);
    }
    
    /// <summary>
    /// Query de área retangular.
    /// </summary>
    public int QueryEntities(Position min, Position max, int floor, Span<Entity> buffer)
    {
        int count = 0;
        int z = Math. Clamp(floor, 0, Floors - 1);
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
    public int QueryEntitiesCircle(Position center, int floor, int radius, Span<Entity> buffer)
    {
        int count = 0;
        int z = Math.Clamp(floor, 0, Floors - 1);
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
    public int GetWalkableNeighbors(Position pos, int floor, Span<Position> neighbors, bool diagonal = false)
    {
        var dirs = diagonal ? AllDirs : CardinalDirs;
        int count = 0;
        
        foreach (var (dx, dy, _) in dirs)
        {
            int nx = pos.X + dx, ny = pos.Y + dy;
            
            if (! InBounds(nx, ny, floor)) continue;
            if (IsBlocked(nx, ny, floor)) continue;
            
            // Evita cortar cantos em diagonais
            if (dx != 0 && dy != 0)
            {
                if (IsBlocked(pos.X + dx, pos.Y, floor) || 
                    IsBlocked(pos. X, pos.Y + dy, floor))
                    continue;
            }
            
            if (count < neighbors.Length)
                neighbors[count++] = new Position { X = nx, Y = ny };
        }
        
        return count;
    }
    
    /// <summary>
    /// Retorna vizinhos walkable E não ocupados. 
    /// </summary>
    public int GetFreeNeighbors(Position pos, int floor, Span<Position> neighbors, bool diagonal = false)
    {
        var dirs = diagonal ? AllDirs : CardinalDirs;
        int count = 0;
        
        foreach (var (dx, dy, _) in dirs)
        {
            int nx = pos.X + dx, ny = pos.Y + dy;
            
            if (!IsWalkableAndFree(nx, ny, floor)) continue;
            
            // Evita cortar cantos
            if (dx != 0 && dy != 0)
            {
                if (IsBlocked(pos.X + dx, pos.Y, floor) || 
                    IsBlocked(pos. X, pos.Y + dy, floor))
                    continue;
            }
            
            if (count < neighbors. Length)
                neighbors[count++] = new Position { X = nx, Y = ny };
        }
        
        return count;
    }
    
    // ========== VALIDAÇÃO DE MOVIMENTO ==========
    
    public MoveResult ValidateMove(Position target, int floor, Entity movingEntity)
    {
        if (!InBounds(target, floor))
            return MoveResult.OutOfBounds;
        
        if (IsBlocked(target, floor))
            return MoveResult.BlockedByTerrain;
        
        if (TryGetFirstEntity(target, floor, out var occupant) && 
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
        if ((uint)z >= (uint)Floors) return;
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
        if ((uint)z >= (uint)Floors) return;
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
        if ((uint)z >= (uint)Floors) return;
        
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
        
        return (Width * Height * Floors, occupied, total);
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