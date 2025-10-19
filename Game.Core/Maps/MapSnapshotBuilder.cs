using System.IO.Compression;
using Game.ECS.Components;

namespace Game.Core.Maps;

/// <summary>
/// Builder para criar MapSnapshots a partir de entidades Map do domínio.
/// </summary>
public static class MapSnapshotBuilder
{
    public const byte CURRENT_VERSION = 1;

    public enum CompressionMode : byte
    {
        None = 0,
        RLE = 1,           // Run-Length Encoding (bom para mapas com áreas uniformes)
        Deflate = 2,       // Compressão Deflate padrão
        MortonRLE = 3      // Morton ordering + RLE (melhor para mapas grandes)
    }

    /// <summary>
    /// Cria um snapshot a partir de um Map usando compressão automática.
    /// </summary>
    public static MapSnapshot CreateSnapshot(
        Domain.Entities.Map map, 
        int mapId,
        CompressionMode compression = CompressionMode.MortonRLE,
        byte[]? metadata = null)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));

        var id = mapId;
        var tiles = ExtractTileData(map);
        var compressed = Compress(tiles, map.Width, map.Height, compression);
        var checksum = ComputeChecksum(compressed);

        return new MapSnapshot
        {
            MapId = id,
            Name = map.Name,
            Width = (ushort)map.Width,
            Height = (ushort)map.Height,
            Layers = (byte)map.Layers,
            BorderBlocked = map.BorderBlocked,
            CompressedTileData = compressed,
            CompressionType = (byte)compression,
            DataChecksum = checksum,
            CreatedAtTicks = DateTime.UtcNow.Ticks,
            Version = CURRENT_VERSION,
            Metadata = metadata
        };
    }

    /// <summary>
    /// Cria um snapshot de uma única layer (otimizado para pathfinding).
    /// </summary>
    public static MapSnapshot CreateLayerSnapshot(
        Domain.Entities.Map map,
        int layer,
        int mapId,
        CompressionMode compression = CompressionMode.MortonRLE)
    {
        if (map == null)
            throw new ArgumentNullException(nameof(map));
        if (layer < 0 || layer >= map.Layers)
            throw new ArgumentOutOfRangeException(nameof(layer));

        var id = mapId;
        var tiles = ExtractLayerData(map, layer);
        var compressed = Compress(tiles, map.Width, map.Height, compression);
        var checksum = ComputeChecksum(compressed);

        return new MapSnapshot
        {
            MapId = id,
            Name = $"{map.Name}_L{layer}",
            Width = (ushort)map.Width,
            Height = (ushort)map.Height,
            Layers = 1,
            BorderBlocked = map.BorderBlocked,
            CompressedTileData = compressed,
            CompressionType = (byte)compression,
            DataChecksum = checksum,
            CreatedAtTicks = DateTime.UtcNow.Ticks,
            Version = CURRENT_VERSION
        };
    }

    private static byte[] ExtractTileData(Domain.Entities.Map map)
    {
        // Cada tile = 2 bytes (Type + CollisionMask)
        var buffer = new byte[map.Count * 2];
        int offset = 0;

        for (int z = 0; z < map.Layers; z++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var tile = map.GetTile(x, y, z);
                    buffer[offset++] = (byte)tile.Type;
                    buffer[offset++] = tile.CollisionMask;
                }
            }
        }

        return buffer;
    }

    private static byte[] ExtractLayerData(Domain.Entities.Map map, int layer)
    {
        var buffer = new byte[map.Width * map.Height * 2];
        int offset = 0;

        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                var tile = map.GetTile(x, y, layer);
                buffer[offset++] = (byte)tile.Type;
                buffer[offset++] = tile.CollisionMask;
            }
        }

        return buffer;
    }

    private static byte[] Compress(byte[] data, int width, int height, CompressionMode mode)
    {
        return mode switch
        {
            CompressionMode.None => data,
            CompressionMode.RLE => CompressRLE(data),
            CompressionMode.Deflate => CompressDeflate(data),
            CompressionMode.MortonRLE => CompressMortonRLE(data, width, height),
            _ => data
        };
    }

    private static byte[] CompressRLE(byte[] data)
    {
        // Run-Length Encoding simples
        using var ms = new MemoryStream();
        
        int i = 0;
        while (i < data.Length)
        {
            byte value = data[i];
            int runLength = 1;

            // Contar quantos bytes iguais seguem
            while (i + runLength < data.Length && 
                   data[i + runLength] == value && 
                   runLength < 255)
            {
                runLength++;
            }

            // Escrever: [run_length][value]
            ms.WriteByte((byte)runLength);
            ms.WriteByte(value);

            i += runLength;
        }

        return ms.ToArray();
    }

    private static byte[] CompressDeflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal))
        {
            deflate.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] CompressMortonRLE(byte[] data, int width, int height)
    {
        // Reordenar dados em Morton order e depois aplicar RLE
        // Tiles próximos espacialmente ficam próximos na memória = melhor compressão
        
        int tileCount = width * height;
        int layers = data.Length / (tileCount * 2);
        var (_, rankToPos) = MortonHelper.BuildMortonMapping(width, height);

        using var ms = new MemoryStream();

        for (int z = 0; z < layers; z++)
        {
            var mortonOrdered = new byte[tileCount * 2];
            int layerOffset = z * tileCount * 2;

            // Reordenar em Morton order
            for (int rank = 0; rank < tileCount; rank++)
            {
                var (x, y) = rankToPos[rank];
                int srcOffset = layerOffset + (y * width + x) * 2;
                int dstOffset = rank * 2;

                mortonOrdered[dstOffset] = data[srcOffset];
                mortonOrdered[dstOffset + 1] = data[srcOffset + 1];
            }

            // Aplicar RLE nos dados ordenados
            var compressed = CompressRLE(mortonOrdered);
            ms.Write(compressed, 0, compressed.Length);
        }

        return ms.ToArray();
    }

    private static uint ComputeChecksum(byte[] data)
    {
        const uint polynomial = 0xEDB88320;
        uint crc = 0xFFFFFFFF;

        foreach (byte b in data)
        {
            crc ^= b;
            for (int i = 0; i < 8; i++)
            {
                crc = (crc & 1) != 0 ? (crc >> 1) ^ polynomial : crc >> 1;
            }
        }

        return ~crc;
    }

    /// <summary>
    /// Estima o tamanho final do snapshot antes de criar.
    /// </summary>
    public static int EstimateSnapshotSize(Domain.Entities.Map map, CompressionMode mode)
    {
        var rawSize = map.Count * 2; // 2 bytes por tile
        
        var compressionRatio = mode switch
        {
            CompressionMode.None => 1.0,
            CompressionMode.RLE => 0.5,      // Estimativa conservadora
            CompressionMode.Deflate => 0.3,
            CompressionMode.MortonRLE => 0.4,
            _ => 1.0
        };

        var dataSize = (int)(rawSize * compressionRatio);
        var overhead = 64; // Headers, metadata, etc.

        return dataSize + overhead;
    }

    /// <summary>
    /// Compara diferentes modos de compressão e retorna estatísticas.
    /// </summary>
    public static CompressionStats CompareCompressionModes(Domain.Entities.Map map)
    {
        var rawData = ExtractTileData(map);
        var modes = Enum.GetValues<CompressionMode>();
        var results = new Dictionary<CompressionMode, (int size, TimeSpan time)>();

        foreach (var mode in modes)
        {
            var start = DateTime.UtcNow;
            var compressed = Compress(rawData, map.Width, map.Height, mode);
            var elapsed = DateTime.UtcNow - start;
            results[mode] = (compressed.Length, elapsed);
        }

        return new CompressionStats(rawData.Length, results);
    }
}

/// <summary>
/// Estatísticas de compressão.
/// </summary>
public record CompressionStats(
    int OriginalSize,
    Dictionary<MapSnapshotBuilder.CompressionMode, (int size, TimeSpan time)> Results)
{
    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Original Size: {OriginalSize:N0} bytes");
        sb.AppendLine("Compression Results:");
        
        foreach (var (mode, (size, time)) in Results.OrderBy(x => x.Value.size))
        {
            var ratio = (double)size / OriginalSize;
            sb.AppendLine($"  {mode,-12} {size,8:N0} bytes ({ratio:P1}) in {time.TotalMilliseconds:F2}ms");
        }

        return sb.ToString();
    }
}