using System.IO.Compression;

namespace Game.DTOs.Map;

/// <summary>
/// Camada de colisão comprimida em bits.
/// 
/// Para um mapa 512x512:
/// - Sem compressão: 262. 144 bytes (1 byte por tile)
/// - BitPacked: 32. 768 bytes (1 bit por tile)
/// - Com RLE/Deflate: ~2-8 KB (dependendo do padrão)
/// 
/// Ideal para enviar mapa inteiro de uma vez (login, teleport).
/// </summary>
public sealed class CollisionLayerDto
{
    public int MapId { get; init; }
    public byte Layer { get; init; }
    public ushort Width { get; init; }
    public ushort Height { get; init; }
    
    /// <summary>
    /// Dados de colisão comprimidos.
    /// Formato: BitPacked + Deflate
    /// </summary>
    public byte[] CompressedData { get; init; } = [];
    
    /// <summary>
    /// Tamanho original (para alocação no cliente).
    /// </summary>
    public int UncompressedBitCount { get; init; }
    
    /// <summary>
    /// Descomprime e retorna array de colisão para o cliente.
    /// </summary>
    public bool[,] Decompress()
    {
        var bitPacked = DecompressBytes(CompressedData);
        var collision = new bool[Width, Height];
        
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int linearIdx = y * Width + x;
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
    public byte[] DecompressLinear()
    {
        var bitPacked = DecompressBytes(CompressedData);
        var collision = new byte[Width * Height];
        
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