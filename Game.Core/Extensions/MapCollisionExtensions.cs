namespace Game.Core.Extensions;

public static class MapCollisionExtensions
{
    /// <summary>
    /// Itera (yield) todas as posições (x,y) cujo collisionMask != 0.
    /// Sem alocar lista (ideal para loops).
    /// </summary>
    public static IEnumerable<(int x, int y)> GetCollidingPositions(this byte[] collisionLayer, int width, int height)
    {
        if (collisionLayer == null) throw new ArgumentNullException(nameof(collisionLayer));
        if (collisionLayer.Length != width * height) throw new ArgumentException("collisionLayer length mismatch", nameof(collisionLayer));

        for (int i = 0; i < collisionLayer.Length; i++)
        {
            if (collisionLayer[i] != 0)
            {
                int x = i % width;
                int y = i / width;
                yield return (x, y);
            }
        }
    }

    /// <summary>
    /// Retorna uma lista com todas as posições colididas (aloca).
    /// Útil quando precisa do resultado indexado/armazenado.
    /// </summary>
    public static List<(int x, int y)> ToCollidingPositionList(this byte[] collisionLayer, int width, int height)
    {
        var list = new List<(int x, int y)>();
        foreach (var pos in collisionLayer.GetCollidingPositions(width, height))
            list.Add(pos);
        return list;
    }

    /// <summary>
    /// Retorna uma matriz bool[height,width] com true = bloqueado.
    /// Útil para algoritmos que preferem grid rápido de indexação.
    /// </summary>
    public static bool[,] ToCollisionGrid(this byte[] collisionLayer, int width, int height)
    {
        if (collisionLayer == null) throw new ArgumentNullException(nameof(collisionLayer));
        if (collisionLayer.Length != width * height) throw new ArgumentException("collisionLayer length mismatch", nameof(collisionLayer));

        var grid = new bool[height, width];
        for (int i = 0; i < collisionLayer.Length; i++)
        {
            if (collisionLayer[i] != 0)
            {
                int x = i % width;
                int y = i / width;
                grid[y, x] = true;
            }
        }
        return grid;
    }

    /// <summary>
    /// Itera posições cujo (collisionMask & mask) != 0 — permite checar bits da máscara.
    /// </summary>
    public static IEnumerable<(int x, int y)> GetPositionsByMask(this byte[] collisionLayer, int width, int height, byte mask)
    {
        if (collisionLayer == null) throw new ArgumentNullException(nameof(collisionLayer));
        if (collisionLayer.Length != width * height) throw new ArgumentException("collisionLayer length mismatch", nameof(collisionLayer));

        for (int i = 0; i < collisionLayer.Length; i++)
        {
            if ((collisionLayer[i] & mask) != 0)
            {
                int x = i % width;
                int y = i / width;
                yield return (x, y);
            }
        }
    }

    /// <summary>
    /// Retorna se a posição (x,y) está bloqueada consultando o array collisionLayer.
    /// </summary>
    public static bool IsBlocked(this byte[] collisionLayer, int width, int height, int x, int y, byte requiredMask = 0xFF)
    {
        if (collisionLayer == null) throw new ArgumentNullException(nameof(collisionLayer));
        if (x < 0 || x >= width || y < 0 || y >= height) return true; // fora do mapa pode ser considerado bloqueado
        int idx = y * width + x;
        var mask = collisionLayer[idx];
        return requiredMask == 0xFF ? mask != 0 : (mask & requiredMask) != 0;
    }

    /// <summary>
    /// Converte o array de colisão em um BitArray-like (bool[]) linear para acesso rápido por index.
    /// Útil quando você quer teste constante sem recomputar x/y.
    /// </summary>
    public static bool[] ToBoolLinear(this byte[] collisionLayer)
    {
        if (collisionLayer == null) throw new ArgumentNullException(nameof(collisionLayer));
        var b = new bool[collisionLayer.Length];
        for (int i = 0; i < collisionLayer.Length; i++)
            b[i] = collisionLayer[i] != 0;
        return b;
    }
}