using System.IO.Compression;
using Game.DTOs.Map;
using Game.ECS.Components;

namespace Game.ECS.Services.Map;

public static class WorldMapDtoExtensions
{
    /// <summary>
    /// Cria DTO de colisão comprimido a partir do WorldMap.
    /// </summary>
    public static CollisionLayerDto FromWorldMap(WorldMap map, int z)
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
                
                if (map.IsBlocked(x, y, z))
                {
                    int byteIdx = linearIdx / 8;
                    int bitIdx = linearIdx % 8;
                    bitPacked[byteIdx] |= (byte)(1 << bitIdx);
                }
            }
        }
        
        // Comprimir com Deflate
        var compressed = CollisionLayerDto.Compress(bitPacked);
        
        return new CollisionLayerDto
        {
            MapId = map. Id,
            Layer = (byte)z,
            Width = (ushort)width,
            Height = (ushort)height,
            CompressedData = compressed,
            UncompressedBitCount = totalBits
        };
    }
    
    public static MapMetadataDto ToDto(this WorldMap map, Position defaultSpawn) => new()
    {
        MapId = map.Id,
        Name = map.Name,
        Width = (ushort)map.Width,
        Height = (ushort)map.Height,
        Layers = (byte)map.Layers,
        Flags = (byte)map.Flags,
        BgmId = map.BgmId,
        DefaultSpawnX = defaultSpawn.X,
        DefaultSpawnY = defaultSpawn.Y,
        DefaultSpawnZ = defaultSpawn.Z
    };
    
}