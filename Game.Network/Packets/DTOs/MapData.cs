using Game.Domain.Enums;
using MemoryPack;

namespace Game.Network.Packets.DTOs;

[MemoryPackable]
public readonly partial struct MapData(string name, int width, int height, TileType[] tileData, byte[] collisionData)
{
    public string Name { get; init; } = name;
    public int Width { get; init; } = width;
    public int Height { get; init; } = height;
    public TileType[] TileData { get; init; } = tileData;
    public byte[] CollisionData { get; init; } = collisionData;
}