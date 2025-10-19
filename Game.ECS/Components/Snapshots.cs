using System.Runtime.InteropServices;
using MemoryPack;

namespace Game.ECS.Components;

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct PlayerInputSnapshot(
    int PlayerId,
    sbyte InputX,
    sbyte InputY,
    InputFlags Flags);

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct PlayerStateSnapshot(
    int PlayerId,
    int PositionX, 
    int PositionY, 
    int PositionZ, 
    int FacingX, 
    int FacingY, 
    float Speed);

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct PlayerVitalsSnapshot(
    int PlayerId, 
    int CurrentHp, 
    int MaxHp, 
    int CurrentMp, 
    int MaxMp
);

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct PlayerDespawnSnapshot(int NetworkId);

[MemoryPackable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly partial record struct MapSnapshot(
    int MapId,
    string Name,
    ushort Width,
    ushort Height,
    byte Layers,
    bool BorderBlocked,
    byte[] CompressedTileData,
    byte CompressionType,
    uint DataChecksum,
    long CreatedAtTicks,
    byte Version,
    byte[]? Metadata)
{
    public int TotalTiles => Width * Height * Layers;
    public DateTime CreatedAt => new DateTime(CreatedAtTicks, DateTimeKind.Utc);
    
    /// <summary>
    /// Tamanho estimado em bytes quando serializado.
    /// </summary>
    public int EstimatedSizeBytes => 
        4 +   // MapId (int)
        (Name?.Length * 2 ?? 0) + 4 + // String (UTF-16 + length prefix)
        2 +   // Width
        2 +   // Height
        1 +   // Layers
        1 +   // BorderBlocked
        (CompressedTileData?.Length ?? 0) + 4 + // Array + length prefix
        1 +   // CompressionType
        4 +   // DataChecksum
        8 +   // CreatedAtTicks
        1 +   // Version
        (Metadata?.Length ?? 0) + 4; // Metadata + length prefix

    /// <summary>
    /// Cria um snapshot vazio (útil para testes).
    /// </summary>
    public static MapSnapshot Empty => new(
        MapId: 0,
        Name: string.Empty,
        Width: 0,
        Height: 0,
        Layers: 0,
        BorderBlocked: false,
        CompressedTileData: Array.Empty<byte>(),
        CompressionType: 0,
        DataChecksum: 0,
        CreatedAtTicks: DateTime.UtcNow.Ticks,
        Version: 1,
        Metadata: null
    );

    /// <summary>
    /// Valida a integridade do snapshot.
    /// </summary>
    public bool IsValid()
    {
        if (Width == 0 || Height == 0 || Layers == 0)
            return false;

        if (CompressedTileData == null || CompressedTileData.Length == 0)
            return false;

        if (CompressionType > 3)
            return false;

        // Validar checksum
        var computedChecksum = ComputeChecksum(CompressedTileData);
        return computedChecksum == DataChecksum;
    }

    private static uint ComputeChecksum(byte[] data)
    {
        // CRC32 simplificado
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

    public override string ToString()
    {
        return $"MapSnapshot[{Name}] {Width}x{Height}x{Layers} | " +
               $"{CompressedTileData?.Length ?? 0} bytes | " +
               $"Compression: {CompressionType} | " +
               $"Valid: {IsValid()}";
    }
}