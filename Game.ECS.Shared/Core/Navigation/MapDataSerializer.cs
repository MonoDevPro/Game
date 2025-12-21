using MemoryPack;
using MemoryPack. Compression;

namespace Game.ECS.Shared.Core.Navigation;

/// <summary>
/// Serialização de MapData com MemoryPack + compressão.
/// </summary>
public static class MapDataSerializer
{
    #region Serialization

    /// <summary>
    /// Serializa MapData (com compressão Brotli).
    /// </summary>
    public static byte[] Serialize(MapData map, bool compress = true)
    {
        // Calcula checksum antes de serializar
        map.Checksum = CalculateChecksum(map);
        
        if (! compress)
            return MemoryPackSerializer. Serialize(map);

        using var compressor = new BrotliCompressor();
        MemoryPackSerializer. Serialize(compressor, map);
        return compressor. ToArray();
    }

    /// <summary>
    /// Deserializa MapData. 
    /// </summary>
    public static MapData?  Deserialize(byte[] data, bool compressed = true)
    {
        MapData? map;
        
        if (compressed)
        {
            using var decompressor = new BrotliDecompressor();
            var decompressed = decompressor.Decompress(data);
            map = MemoryPackSerializer. Deserialize<MapData>(decompressed);
        }
        else
        {
            map = MemoryPackSerializer.Deserialize<MapData>(data);
        }

        // Valida checksum
        if (map != null)
        {
            uint expectedChecksum = map.Checksum;
            map.Checksum = 0;
            uint actualChecksum = CalculateChecksum(map);
            map.Checksum = expectedChecksum;

            if (expectedChecksum != actualChecksum)
                throw new InvalidDataException("Map checksum mismatch - data may be corrupted");
        }

        return map;
    }

    #endregion

    #region Conversion

    /// <summary>
    /// Cria MapData a partir de um NavigationGrid.
    /// </summary>
    public static MapData FromGrid(NavigationGrid grid, string id, string name)
    {
        return new MapData
        {
            Id = id,
            Name = name,
            Width = (ushort)grid.Width,
            Height = (ushort)grid.Height,
            CellSize = grid.CellSize,
            WalkabilityData = RleEncode(grid.ToBytes()),
            Version = 1
        };
    }

    /// <summary>
    /// Cria NavigationGrid a partir de MapData.
    /// </summary>
    public static NavigationGrid ToGrid(MapData map)
    {
        var grid = new NavigationGrid(map.Width, map.Height, map. CellSize);
        var walkability = RleDecode(map.WalkabilityData, map.Width * map.Height);
        grid.LoadFromBytes(walkability);
        return grid;
    }

    #endregion

    #region File I/O

    /// <summary>
    /// Salva MapData em arquivo.
    /// </summary>
    public static async Task SaveAsync(MapData map, string filePath)
    {
        var data = Serialize(map);
        await File.WriteAllBytesAsync(filePath, data);
    }

    /// <summary>
    /// Carrega MapData de arquivo.
    /// </summary>
    public static async Task<MapData? > LoadAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return null;

        var data = await File.ReadAllBytesAsync(filePath);
        return Deserialize(data);
    }

    /// <summary>
    /// Versões síncronas. 
    /// </summary>
    public static void Save(MapData map, string filePath)
    {
        var data = Serialize(map);
        File.WriteAllBytes(filePath, data);
    }

    public static MapData?  Load(string filePath)
    {
        if (!File. Exists(filePath))
            return null;

        var data = File.ReadAllBytes(filePath);
        return Deserialize(data);
    }

    #endregion

    #region RLE Encoding

    private static byte[] RleEncode(byte[] data)
    {
        using var ms = new MemoryStream();

        int i = 0;
        while (i < data.Length)
        {
            byte value = data[i];
            int count = 1;

            while (i + count < data.Length && 
                   data[i + count] == value && 
                   count < 255)
            {
                count++;
            }

            ms. WriteByte((byte)count);
            ms.WriteByte(value);
            i += count;
        }

        return ms.ToArray();
    }

    private static byte[] RleDecode(byte[] encoded, int expectedLength)
    {
        var result = new byte[expectedLength];
        int writeIdx = 0;

        for (int i = 0; i < encoded.Length && writeIdx < expectedLength; i += 2)
        {
            int count = encoded[i];
            byte value = encoded[i + 1];

            for (int j = 0; j < count && writeIdx < expectedLength; j++)
            {
                result[writeIdx++] = value;
            }
        }

        return result;
    }

    #endregion

    #region Checksum

    private static uint CalculateChecksum(MapData map)
    {
        uint hash = 2166136261;
        
        hash = HashString(hash, map.Id);
        hash = HashString(hash, map.Name);
        hash = HashValue(hash, map. Width);
        hash = HashValue(hash, map.Height);
        hash = HashBytes(hash, map. WalkabilityData);
        hash = HashValue(hash, (uint)map.SpawnPoints.Length);
        hash = HashValue(hash, (uint)map.Zones.Length);
        hash = HashValue(hash, (uint)map.Portals.Length);
        hash = HashValue(hash, map. Version);

        return hash;
    }

    private static uint HashString(uint hash, string str)
    {
        foreach (char c in str)
        {
            hash ^= c;
            hash *= 16777619;
        }
        return hash;
    }

    private static uint HashValue(uint hash, uint value)
    {
        hash ^= value;
        hash *= 16777619;
        return hash;
    }

    private static uint HashBytes(uint hash, byte[] bytes)
    {
        foreach (byte b in bytes)
        {
            hash ^= b;
            hash *= 16777619;
        }
        return hash;
    }

    #endregion
}