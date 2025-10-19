using System.IO.Compression;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.ECS.Components;
using Game.ECS.Services;
using Game.Network.Packets.Simulation;

namespace Game.Core.Maps;

/// <summary>
/// Carrega MapSnapshots e reconstrói MapGrid no cliente.
/// </summary>
public static class MapSnapshotLoader
{
    /// <summary>
    /// Carrega um snapshot e cria um IMapGrid pronto para uso.
    /// </summary>
    public static IMapGrid LoadMapGrid(MapSnapshot snapshot)
    {
        if (!snapshot.IsValid())
            throw new InvalidDataException("Invalid snapshot data");

        var map = LoadMap(snapshot);
        return MapGridFactory.Create(map);
    }

    /// <summary>
    /// Carrega um snapshot e cria um Map do domínio.
    /// </summary>
    public static Map LoadMap(MapSnapshot snapshot)
    {
        if (!snapshot.IsValid())
            throw new InvalidDataException("Invalid snapshot data");

        var map = new Map(
            snapshot.Name,
            snapshot.Width,
            snapshot.Height,
            snapshot.Layers,
            snapshot.BorderBlocked
        );

        var decompressed = Decompress(
            snapshot.CompressedTileData,
            snapshot.Width,
            snapshot.Height,
            snapshot.Layers,
            (MapSnapshotBuilder.CompressionMode)snapshot.CompressionType
        );

        PopulateMap(map, decompressed);
        return map;
    }

    /// <summary>
    /// Carrega apenas a camada de colisão (otimizado para pathfinding).
    /// </summary>
    public static byte[] LoadCollisionLayer(MapSnapshot snapshot, int layer = 0)
    {
        if (!snapshot.IsValid())
            throw new InvalidDataException("Invalid snapshot data");

        if (layer >= snapshot.Layers)
            throw new ArgumentOutOfRangeException(nameof(layer));

        var decompressed = Decompress(
            snapshot.CompressedTileData,
            snapshot.Width,
            snapshot.Height,
            snapshot.Layers,
            (MapSnapshotBuilder.CompressionMode)snapshot.CompressionType
        );

        return ExtractCollisionLayer(decompressed, snapshot.Width, snapshot.Height, layer);
    }

    private static byte[] Decompress(
        byte[] compressed,
        int width,
        int height,
        int layers,
        MapSnapshotBuilder.CompressionMode mode)
    {
        return mode switch
        {
            MapSnapshotBuilder.CompressionMode.None => compressed,
            MapSnapshotBuilder.CompressionMode.RLE => DecompressRLE(compressed, width * height * layers * 2),
            MapSnapshotBuilder.CompressionMode.Deflate => DecompressDeflate(compressed),
            MapSnapshotBuilder.CompressionMode.MortonRLE => DecompressMortonRLE(compressed, width, height, layers),
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
            // Descomprimir camada em Morton order
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

            // Converter de Morton order para ordem linear (XY)
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
            // Pular Type byte (offset + i*2), pegar apenas CollisionMask
            collision[i] = data[layerOffset + i * 2 + 1];
        }

        return collision;
    }
}