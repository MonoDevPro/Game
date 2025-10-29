using System;
using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct MapDto(
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
    public const byte CURRENT_VERSION = 1;

    public int TotalTiles => Width * Height * Layers;
    public DateTime CreatedAt => new(CreatedAtTicks, DateTimeKind.Utc);

    /// <summary>
    /// Tamanho estimado em bytes quando serializado.
    /// (Estimativa simples; só para referência.)
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
    /// Snapshot vazio (útil para testes).
    /// </summary>
    public static MapDto Empty => new(
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
        Version: CURRENT_VERSION,
        Metadata: null
    );

    /// <summary>
    /// Modos de compressão (mantenho aqui para compatibilidade).
    /// </summary>
    public enum CompressionMode : byte
    {
        None = 0,
        RLE = 1,
        Deflate = 2,
        MortonRLE = 3
    }
}