using System.IO.Compression;
using Game.Domain;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Network.Packets.Game;

namespace Game.Core.Extensions;

/// <summary>
/// Extensions e helpers relacionados a MapDto:
/// - Criação de snapshot a partir de Map
/// - Compressão / descompressão
/// - Checksum
/// - Carregamento de Map a partir de MapDto
/// </summary>
public static class MapDtoExtensions
{
    #region Factories (Map -> MapDto)

    public static MapDataPacket ToMapDto(
        this Map map,
        int mapId,
        MapDataPacket.CompressionMode compression = MapDataPacket.CompressionMode.MortonRLE,
        byte[]? metadata = null)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));

        var tiles = ExtractTileData(map);
        var compressed = Compress(tiles, map.Width, map.Height, compression);
        var checksum = ComputeChecksum(compressed);

        return new MapDataPacket(
            MapId: mapId,
            Name: map.Name,
            Width: (ushort)map.Width,
            Height: (ushort)map.Height,
            Layers: (byte)map.Layers,
            BorderBlocked: map.BorderBlocked,
            CompressedTileData: compressed,
            CompressionType: (byte)compression,
            DataChecksum: checksum,
            CreatedAtTicks: DateTime.UtcNow.Ticks,
            Version: MapDataPacket.CURRENT_VERSION,
            Metadata: metadata
        );
    }

    public static MapDataPacket ToLayerMapDto(
        this Map map,
        int layer,
        int mapId,
        MapDataPacket.CompressionMode compression = MapDataPacket.CompressionMode.MortonRLE)
    {
        if (map == null) throw new ArgumentNullException(nameof(map));
        if (layer < 0 || layer >= map.Layers) throw new ArgumentOutOfRangeException(nameof(layer));

        var tiles = ExtractLayerData(map, layer);
        var compressed = Compress(tiles, map.Width, map.Height, compression);
        var checksum = ComputeChecksum(compressed);

        return new MapDataPacket(
            MapId: mapId,
            Name: $"{map.Name}_L{layer}",
            Width: (ushort)map.Width,
            Height: (ushort)map.Height,
            Layers: 1,
            BorderBlocked: map.BorderBlocked,
            CompressedTileData: compressed,
            CompressionType: (byte)compression,
            DataChecksum: checksum,
            CreatedAtTicks: DateTime.UtcNow.Ticks,
            Version: MapDataPacket.CURRENT_VERSION,
            Metadata: null
        );
    }

    #endregion

    #region Validation / Helpers for MapDto

    public static bool IsValid(this MapDataPacket dataPacket)
    {
        if (dataPacket.Width == 0 || dataPacket.Height == 0 || dataPacket.Layers == 0) return false;
        if (dataPacket.CompressedTileData.Length == 0) return false;
        if (dataPacket.CompressionType > (byte)MapDataPacket.CompressionMode.MortonRLE) return false;

        var computed = ComputeChecksum(dataPacket.CompressedTileData);
        return computed == dataPacket.DataChecksum;
    }

    public static Map LoadMap(this MapDataPacket dataPacket)
    {
        if (!dataPacket.IsValid())
            throw new InvalidDataException("Invalid snapshot data");

        var map = new Map(dataPacket.Name, dataPacket.Width, dataPacket.Height, dataPacket.Layers, dataPacket.BorderBlocked);

        var decompressed = Decompress(
            dataPacket.CompressedTileData,
            dataPacket.Width,
            dataPacket.Height,
            dataPacket.Layers,
            (MapDataPacket.CompressionMode)dataPacket.CompressionType
        );

        PopulateMap(map, decompressed);
        return map;
    }

    public static byte[] LoadCollisionLayer(this MapDataPacket dataPacket, int layer = 0)
    {
        if (!dataPacket.IsValid())
            throw new InvalidDataException("Invalid snapshot data");
        if (layer >= dataPacket.Layers) throw new ArgumentOutOfRangeException(nameof(layer));

        var decompressed = Decompress(
            dataPacket.CompressedTileData,
            dataPacket.Width,
            dataPacket.Height,
            dataPacket.Layers,
            (MapDataPacket.CompressionMode)dataPacket.CompressionType
        );

        return ExtractCollisionLayer(decompressed, dataPacket.Width, dataPacket.Height, layer);
    }

    #endregion

    #region Extraction helpers (Map -> raw byte[])

    private static byte[] ExtractTileData(Map map)
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

    private static byte[] ExtractLayerData(Map map, int layer)
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

    #endregion

    #region Compression / Decompression

    private static byte[] Compress(byte[] data, int width, int height, MapDataPacket.CompressionMode mode)
    {
        return mode switch
        {
            MapDataPacket.CompressionMode.None => data,
            MapDataPacket.CompressionMode.RLE => CompressRLE(data),
            MapDataPacket.CompressionMode.Deflate => CompressDeflate(data),
            MapDataPacket.CompressionMode.MortonRLE => CompressMortonRLE(data, width, height),
            _ => data
        };
    }

    private static byte[] CompressRLE(byte[] data)
    {
        using var ms = new MemoryStream();

        int i = 0;
        while (i < data.Length)
        {
            byte value = data[i];
            int runLength = 1;
            while (i + runLength < data.Length && data[i + runLength] == value && runLength < 255)
            {
                runLength++;
            }
            ms.WriteByte((byte)runLength);
            ms.WriteByte(value);
            i += runLength;
        }

        return ms.ToArray();
    }

    private static byte[] CompressDeflate(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflate = new DeflateStream(output, CompressionLevel.Optimal, leaveOpen: true))
        {
            deflate.Write(data, 0, data.Length);
        }
        return output.ToArray();
    }

    private static byte[] CompressMortonRLE(byte[] data, int width, int height)
    {
        int tileCount = width * height;
        int layers = data.Length / (tileCount * 2);
        var (_, rankToPos) = MortonHelper.BuildMortonMapping(width, height);

        using var ms = new MemoryStream();

        for (int z = 0; z < layers; z++)
        {
            var mortonOrdered = new byte[tileCount * 2];
            int layerOffset = z * tileCount * 2;

            for (int rank = 0; rank < tileCount; rank++)
            {
                var (x, y) = rankToPos[rank];
                int srcOffset = layerOffset + (y * width + x) * 2;
                int dstOffset = rank * 2;

                mortonOrdered[dstOffset] = data[srcOffset];
                mortonOrdered[dstOffset + 1] = data[srcOffset + 1];
            }

            var compressed = CompressRLE(mortonOrdered);
            ms.Write(compressed, 0, compressed.Length);
        }

        return ms.ToArray();
    }

    private static byte[] Decompress(
        byte[] compressed,
        int width,
        int height,
        int layers,
        MapDataPacket.CompressionMode mode)
    {
        return mode switch
        {
            MapDataPacket.CompressionMode.None => compressed,
            MapDataPacket.CompressionMode.RLE => DecompressRLE(compressed, width * height * layers * 2),
            MapDataPacket.CompressionMode.Deflate => DecompressDeflate(compressed),
            MapDataPacket.CompressionMode.MortonRLE => DecompressMortonRLE(compressed, width, height, layers),
            _ => throw new NotSupportedException($"Compression mode {mode} not supported")
        };
    }

    private static byte[] DecompressRLE(byte[] compressed, int expectedSize)
    {
        var output = new byte[expectedSize];
        int srcIdx = 0;
        int dstIdx = 0;

        while (srcIdx < compressed.Length && dstIdx < expectedSize)
        {
            byte runLength = compressed[srcIdx++];
            byte value = compressed[srcIdx++];

            for (int i = 0; i < runLength && dstIdx < expectedSize; i++)
            {
                output[dstIdx++] = value;
            }
        }

        return output;
    }

    private static byte[] DecompressDeflate(byte[] compressed)
    {
        using var input = new MemoryStream(compressed);
        using var deflate = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();

        deflate.CopyTo(output);
        return output.ToArray();
    }

    private static byte[] DecompressMortonRLE(byte[] compressed, int width, int height, int layers)
    {
        int tileCount = width * height;
        var (posToRank, _) = MortonHelper.BuildMortonMapping(width, height);
        var output = new byte[tileCount * layers * 2];

        int srcIdx = 0;

        for (int z = 0; z < layers; z++)
        {
            var mortonOrdered = new byte[tileCount * 2];
            int dstIdx = 0;

            while (dstIdx < mortonOrdered.Length && srcIdx < compressed.Length)
            {
                byte runLength = compressed[srcIdx++];
                byte value = compressed[srcIdx++];

                for (int i = 0; i < runLength && dstIdx < mortonOrdered.Length; i++)
                {
                    mortonOrdered[dstIdx++] = value;
                }
            }

            int layerOffset = z * tileCount * 2;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pos = y * width + x;
                    int rank = posToRank[pos];
                    int srcOffset = rank * 2;
                    int dstOffset = layerOffset + pos * 2;

                    output[dstOffset] = mortonOrdered[srcOffset];
                    output[dstOffset + 1] = mortonOrdered[srcOffset + 1];
                }
            }
        }

        return output;
    }

    #endregion

    #region Checksum & population

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

    private static void PopulateMap(Map map, byte[] data)
    {
        int offset = 0;

        for (int z = 0; z < map.Layers; z++)
        {
            for (int y = 0; y < map.Height; y++)
            {
                for (int x = 0; x < map.Width; x++)
                {
                    var tile = new Tile
                    {
                        Type = (TileType)data[offset++],
                        CollisionMask = data[offset++]
                    };
                    map.SetTile(x, y, z, tile);
                }
            }
        }
    }

    private static byte[] ExtractCollisionLayer(byte[] data, int width, int height, int layer)
    {
        int tileCount = width * height;
        var collision = new byte[tileCount];
        int layerOffset = layer * tileCount * 2;

        for (int i = 0; i < tileCount; i++)
        {
            collision[i] = data[layerOffset + i * 2 + 1];
        }

        return collision;
    }

    #endregion

    #region Diagnostics (opcional)

    /// <summary>
    /// Compara modos de compressão (retorna estatísticas).
    /// </summary>
    public static CompressionStats CompareCompressionModes(this Map map)
    {
        var rawData = ExtractTileData(map);
        var modes = Enum.GetValues<MapDataPacket.CompressionMode>();
        var results = new Dictionary<MapDataPacket.CompressionMode, (int size, TimeSpan time)>();

        foreach (var mode in modes)
        {
            var start = DateTime.UtcNow;
            var compressed = Compress(rawData, map.Width, map.Height, mode);
            var elapsed = DateTime.UtcNow - start;
            results[mode] = (compressed.Length, elapsed);
        }

        return new CompressionStats(rawData.Length, results);
    }

    public record CompressionStats(
        int OriginalSize,
        Dictionary<MapDataPacket.CompressionMode, (int size, TimeSpan time)> Results)
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

    #endregion
}
