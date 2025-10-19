using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.Core.Maps;

/// <summary>
/// Implementação de grid de mapa compartilhada entre cliente e servidor.
/// Encapsula a entidade de domínio Map e fornece operações de navegação e colisão.
/// </summary>
public sealed class MapGrid : IMapGrid
{
    private readonly Map _map;
    private readonly int _width;
    private readonly int _height;
    private readonly int _layers;
    private readonly bool _borderBlocked;

    public string Name => _map.Name;
    public int Width => _width;
    public int Height => _height;
    public int Layers => _layers;
    public bool BorderBlocked => _borderBlocked;

    public MapGrid(Map map)
    {
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _width = map.Width;
        _height = map.Height;
        _layers = map.Layers;
        _borderBlocked = map.BorderBlocked;
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
        // Se BorderBlocked está ativo e está fora dos limites, considerar bloqueado
        if (!InBounds(p))
            return _borderBlocked;

        // Verificar se o tile possui máscara de colisão
        if (!_map.TryGetTile(p.X, p.Y, p.Z, out var tile))
            return _borderBlocked;

        return tile.CollisionMask != 0;
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

        // Verificar cada tile na área (assumindo camada Z = 0 para pathfinding)
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (_map.TryGetTile(x, y, 0, out var tile) && tile.CollisionMask != 0)
                    return true;
            }
        }

        return false;
    }

    public int CountBlockedInArea(int minX, int minY, int maxX, int maxY)
    {
        // Normalizar coordenadas
        if (minX > maxX) (minX, maxX) = (maxX, minX);
        if (minY > maxY) (minY, maxY) = (maxY, minY);

        int count = 0;

        // Se BorderBlocked está ativo, contar tiles fora dos limites
        if (_borderBlocked)
        {
            // Contar tiles fora do limite superior/esquerdo
            if (minX < 0 || minY < 0)
            {
                int oobWidth = Math.Max(0, 1 - minX);
                int oobHeight = Math.Max(0, 1 - minY);
                count += (maxX - minX + 1) * oobHeight + oobWidth * (maxY - minY + 1 - oobHeight);
            }

            // Contar tiles fora do limite inferior/direito
            if (maxX >= _width || maxY >= _height)
            {
                int oobWidth = Math.Max(0, maxX - _width + 1);
                int oobHeight = Math.Max(0, maxY - _height + 1);
                count += (maxX - minX + 1) * oobHeight + oobWidth * (maxY - minY + 1 - oobHeight);
            }

            // Ajustar para área válida
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(_width - 1, maxX);
            maxY = Math.Min(_height - 1, maxY);
        }
        else
        {
            // Clampar para os limites do mapa
            minX = Math.Max(0, minX);
            minY = Math.Max(0, minY);
            maxX = Math.Min(_width - 1, maxX);
            maxY = Math.Min(_height - 1, maxY);
        }

        // Contar tiles bloqueados na área válida (assumindo camada Z = 0)
        for (int y = minY; y <= maxY; y++)
        {
            for (int x = minX; x <= maxX; x++)
            {
                if (_map.TryGetTile(x, y, 0, out var tile) && tile.CollisionMask != 0)
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

        if (!_map.TryGetTile(x, y, z, out var tile))
            return _borderBlocked;

        return tile.CollisionMask != 0;
    }

    /// <summary>
    /// Obtém o tipo de tile em uma posição (para lógica de gameplay específica).
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
    /// Verifica se há linha de visão entre duas posições (Bresenham simplificado).
    /// </summary>
    public bool HasLineOfSight(in Position from, in Position to)
    {
        if (!InBounds(from) || !InBounds(to))
            return false;

        // Apenas verifica na mesma camada Z
        if (from.Z != to.Z)
            return false;

        int dx = Math.Abs(to.X - from.X);
        int dy = Math.Abs(to.Y - from.Y);
        int sx = from.X < to.X ? 1 : -1;
        int sy = from.Y < to.Y ? 1 : -1;
        int err = dx - dy;

        int x = from.X;
        int y = from.Y;
        int z = from.Z;

        while (x != to.X || y != to.Y)
        {
            if (IsBlockedAt(x, y, z))
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
    /// Calcula a distância Manhattan entre duas posições.
    /// </summary>
    public static int ManhattanDistance(in Position a, in Position b)
    {
        return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y) + Math.Abs(a.Z - b.Z);
    }

    /// <summary>
    /// Calcula a distância Euclidiana ao quadrado (evita sqrt para performance).
    /// </summary>
    public static int DistanceSquared(in Position a, in Position b)
    {
        int dx = a.X - b.X;
        int dy = a.Y - b.Y;
        int dz = a.Z - b.Z;
        return dx * dx + dy * dy + dz * dz;
    }

    #endregion
}