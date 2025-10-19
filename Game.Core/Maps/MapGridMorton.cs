using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.Core.Maps;

/// <summary>
/// Implementação de grid de mapa usando Morton codes (Z-order curve) para melhor cache locality.
/// Compartilhada entre cliente e servidor.
/// </summary>
public sealed class MapGridMorton : IMapGrid
{
    private readonly Map _map;
    private readonly int _width;
    private readonly int _height;
    private readonly int _layers;
    private readonly bool _borderBlocked;

    // Morton mapping para acesso cache-friendly
    private readonly int[] _posToRank;
    private readonly (int x, int y)[] _rankToPos;
    
    // Cache de colisão por layer (acesso rápido sem consultar Tile completo)
    private readonly byte[][] _collisionByLayer;

    public string Name => _map.Name;
    public int Width => _width;
    public int Height => _height;
    public int Layers => _layers;
    public bool BorderBlocked => _borderBlocked;

    public MapGridMorton(Map map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _width = map.Width;
        _height = map.Height;
        _layers = map.Layers;
        _borderBlocked = map.BorderBlocked;

        // Build Morton mapping
        (_posToRank, _rankToPos) = MortonHelper.BuildMortonMapping(_width, _height);

        // Build collision cache (Morton-ordered)
        _collisionByLayer = new byte[_layers][];
        for (int z = 0; z < _layers; z++)
        {
            _collisionByLayer[z] = BuildCollisionCache(z);
        }
    }

    private byte[] BuildCollisionCache(int layer)
    {
        int count = _width * _height;
        var cache = new byte[count];

        // Fill in Morton order for better cache locality
        for (int rank = 0; rank < count; rank++)
        {
            var (x, y) = _rankToPos[rank];
            if (_map.TryGetTile(x, y, layer, out var tile))
            {
                cache[rank] = tile.CollisionMask;
            }
        }

        return cache;
    }

    #region IMapGrid Implementation

    public bool InBounds(int x, int y, int z)
    {
        return (uint)x < (uint)_width 
            && (uint)y < (uint)_height 
            && (uint)z < (uint)_layers;
    }

    public bool InBounds(in Position p)
    {
        return InBounds(p.X, p.Y, p.Z);
    }

    public Position ClampToBounds(in Position p)
    {
        return new Position
        {
            X = Math.Clamp(p.X, 0, _width - 1),
            Y = Math.Clamp(p.Y, 0, _height - 1),
            Z = Math.Clamp(p.Z, 0, _layers - 1)
        };
    }

    public bool IsBlocked(in Position p)
    {
        if (!InBounds(p))
            return _borderBlocked;

        // Fast lookup via Morton-ordered cache
        int pos = p.Y * _width + p.X;
        int rank = _posToRank[pos];
        return _collisionByLayer[p.Z][rank] != 0;
    }

    public bool AnyBlockedInArea(int minX, int minY, int maxX, int maxY)
    {
        // Normalizar coordenadas
        if (minX > maxX) (minX, maxX) = (maxX, minX);
        if (minY > maxY) (minY, maxY) = (maxY, minY);

        // Se BorderBlocked está ativo, verificar se área sai dos limites
        if (_borderBlocked)
        {
            if (minX < 0 || minY < 0 || maxX >= _width || maxY >= _height)
                return true;
        }
        else
        {
            // Clampar para os limites do mapa
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(_width - 1, maxX);
            maxY = Math.Min(_height - 1, maxY);
        }

        // OTIMIZAÇÃO: Query em Morton order para melhor cache hit
        // Construir lista de ranks na área e verificar em ordem
        int areaWidth = maxX - minX + 1;
        int areaHeight = maxY - minY + 1;
        
        // Para áreas pequenas (< 64 tiles), iteração direta é mais rápida
        if (areaWidth * areaHeight <= 64)
        {
            return AnyBlockedInAreaDirect(minX, minY, maxX, maxY);
        }

        // Para áreas maiores, usar Morton order
        return AnyBlockedInAreaMorton(minX, minY, maxX, maxY);
    }

    private bool AnyBlockedInAreaDirect(int minX, int minY, int maxX, int maxY)
    {
        var collision = _collisionByLayer[0]; // Layer 0 para pathfinding
        
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int pos = y * _width + x;
                int rank = _posToRank[pos];
                if (collision[rank] != 0)
                    return true;
            }
        }
        return false;
    }

    private bool AnyBlockedInAreaMorton(int minX, int minY, int maxX, int maxY)
    {
        var collision = _collisionByLayer[0]; // Layer 0 para pathfinding
        
        // Coletar ranks na área e ordenar por Morton
        Span<int> ranks = stackalloc int[Math.Min(256, (maxX - minX + 1) * (maxY - minY + 1))];
        int rankCount = 0;

        for (int y = minY; y <= maxY && rankCount < ranks.Length; y++)
        {
            for (int x = minX; x <= maxX && rankCount < ranks.Length; x++)
            {
                int pos = y * _width + x;
                ranks[rankCount++] = _posToRank[pos];
            }
        }

        // Ordenar ranks para acesso sequencial (já estão em Morton order implicitamente)
        ranks = ranks[..rankCount];
        ranks.Sort();

        // Verificar em ordem sequencial (melhor cache locality)
        foreach (int rank in ranks)
        {
            if (collision[rank] != 0)
                return true;
        }

        return false;
    }

    public int CountBlockedInArea(int minX, int minY, int maxX, int maxY)
    {
        // Normalizar coordenadas
        if (minX > maxX) (minX, maxX) = (maxX, minX);
        if (minY > maxY) (minY, maxY) = (maxY, minY);

        int count = 0;
        var collision = _collisionByLayer[0];

        // Se BorderBlocked está ativo, contar tiles fora dos limites
        if (_borderBlocked)
        {
            int totalArea = (maxX - minX + 1) * (maxY - minY + 1);
            int validMinX = Math.Max(0, minX);
            int validMinY = Math.Max(0, minY);
            int validMaxX = Math.Min(_width - 1, maxX);
            int validMaxY = Math.Min(_height - 1, maxY);
            int validArea = Math.Max(0, (validMaxX - validMinX + 1) * (validMaxY - validMinY + 1));
            
            count = totalArea - validArea; // Tiles fora = bloqueados
            
            minX = validMinX;
            minY = validMinY;
            maxX = validMaxX;
            maxY = validMaxY;
        }
        else
        {
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(_width - 1, maxX);
            maxY = Math.Min(_height - 1, maxY);
        }

        // Contar bloqueados na área válida (Morton-ordered)
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int pos = y * _width + x;
                int rank = _posToRank[pos];
                if (collision[rank] != 0)
                    count++;
            }
        }

        return count;
    }

    #endregion

    #region Additional Helper Methods

    /// <summary>
    /// Verifica se uma posição é bloqueada em uma camada específica.
    /// </summary>
    public bool IsBlockedAt(int x, int y, int z)
    {
        if (!InBounds(x, y, z))
            return _borderBlocked;

        int pos = y * _width + x;
        int rank = _posToRank[pos];
        return _collisionByLayer[z][rank] != 0;
    }

    /// <summary>
    /// Obtém o tipo de tile em uma posição (consulta o Map original).
    /// </summary>
    public bool TryGetTileType(in Position p, out TileType type)
    {
        if (!InBounds(p))
        {
            type = default;
            return false;
        }

        if (_map.TryGetTile(p.X, p.Y, p.Z, out var tile))
        {
            type = tile.Type;
            return true;
        }

        type = default;
        return false;
    }

    /// <summary>
    /// Verifica linha de visão entre duas posições (Bresenham).
    /// OTIMIZADO: usa cache de colisão Morton-ordered.
    /// </summary>
    public bool HasLineOfSight(in Position from, in Position to)
    {
        if (!InBounds(from) || !InBounds(to))
            return false;

        if (from.Z != to.Z)
            return false;

        var collision = _collisionByLayer[from.Z];

        int dx = Math.Abs(to.X - from.X);
        int dy = Math.Abs(to.Y - from.Y);
        int sx = from.X < to.X ? 1 : -1;
        int sy = from.Y < to.Y ? 1 : -1;
        int err = dx - dy;

        int x = from.X;
        int y = from.Y;

        while (x != to.X || y != to.Y)
        {
            int pos = y * _width + x;
            int rank = _posToRank[pos];
            
            if (collision[rank] != 0)
                return false;

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x += sx;
            }
            if (e2 < dx)
            {
                err += dx;
                y += sy;
            }
        }

        return true;
    }

    /// <summary>
    /// Atualiza o cache de colisão quando o mapa muda (ex: porta abre/fecha).
    /// </summary>
    public void UpdateCollision(int x, int y, int z, byte collisionMask)
    {
        if (!InBounds(x, y, z))
            return;

        int pos = y * _width + x;
        int rank = _posToRank[pos];
        _collisionByLayer[z][rank] = collisionMask;

        // Opcionalmente, sincronizar com o Map original
        if (_map.TryGetTile(x, y, z, out var tile))
        {
            tile.CollisionMask = collisionMask;
            _map.SetTile(x, y, z, tile);
        }
    }

    /// <summary>
    /// Itera sobre área em Morton order (melhor cache locality).
    /// </summary>
    public IEnumerable<(int x, int y, byte collision)> EnumerateAreaMortonOrder(
        int minX, int minY, int maxX, int maxY, int layer = 0)
    {
        if ((uint)layer >= (uint)_layers)
            yield break;

        minX = Math.Max(0, Math.Min(minX, maxX));
        minY = Math.Max(0, Math.Min(minY, maxY));
        maxX = Math.Min(_width - 1, Math.Max(minX, maxX));
        maxY = Math.Min(_height - 1, Math.Max(minY, maxY));

        var collision = _collisionByLayer[layer];
        
        // Coletar posições e ordenar por rank (Morton order)
        var positions = new List<(int rank, int x, int y)>();
        
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                int pos = y * _width + x;
                int rank = _posToRank[pos];
                positions.Add((rank, x, y));
            }
        }

        positions.Sort((a, b) => a.rank.CompareTo(b.rank));

        foreach (var (rank, x, y) in positions)
        {
            yield return (x, y, collision[rank]);
        }
    }

    public static int ManhattanDistance(in Position a, in Position b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
    }

    public static int DistanceSquared(in Position a, in Position b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        int dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    #endregion
}