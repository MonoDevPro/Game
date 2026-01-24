using System.IO.Compression;
using Game.Contracts;

namespace Game.Infrastructure.ArchECS.Services.Map;

public static class MapCompressor
{
    /// <summary>
    /// Cria DTO de colisão comprimido a partir do WorldMap.
    /// </summary>
    public static WorldCollisions FromWorldMap(this WorldMap map, int floor)
    {
        int width = map.Width;
        int height = map.Height;
        int totalBits = width * height;
        int byteCount = (totalBits + 7) / 8;
        
        // Pack bits (1 = blocked, 0 = free)
        var bitPacked = new byte[byteCount];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int linearIdx = y * width + x;
                
                if (map.IsBlocked(x, y, floor))
                {
                    int byteIdx = linearIdx / 8;
                    int bitIdx = linearIdx % 8;
                    bitPacked[byteIdx] |= (byte)(1 << bitIdx);
                }
            }
        }
        
        // Comprimir com Deflate
        var compressed = MapCompressor.Compress(bitPacked);
        
        return new WorldCollisions
        {
            MapId = map. Id,
            Floor = (byte)floor,
            Width = (ushort)width,
            Height = (ushort)height,
            CompressedData = compressed,
            UncompressedBitCount = totalBits
        };
    }
    
    /// <summary>
    /// Descomprime e retorna array de colisão para o cliente.
    /// </summary>
    public static bool[,] Decompress(int width, int height, byte[] compressed)
    {
        var bitPacked = DecompressBytes(compressed);
        var collision = new bool[width, height];
        
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int linearIdx = y * width + x;
                int byteIdx = linearIdx / 8;
                int bitIdx = linearIdx % 8;
                
                collision[x, y] = (bitPacked[byteIdx] & (1 << bitIdx)) != 0;
            }
        }
        
        return collision;
    }
    
    /// <summary>
    /// Descomprime para array linear (mais eficiente para cliente).
    /// </summary>
    public static byte[] DecompressLinear(int width, int height, byte[] compressed)
    {
        var bitPacked = DecompressBytes(compressed);
        var collision = new byte[width * height];
        
        for (int i = 0; i < collision.Length; i++)
        {
            int byteIdx = i / 8;
            int bitIdx = i % 8;
            collision[i] = (byte)((bitPacked[byteIdx] >> bitIdx) & 1);
        }
        
        return collision;
    }
    
    public static byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
        {
            deflate.Write(data);
        }
        return output.ToArray();
    }
    
    public static byte[] DecompressBytes(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode. Decompress);
        using var output = new MemoryStream();
        deflate.CopyTo(output);
        return output.ToArray();
    }
}