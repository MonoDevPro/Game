using MemoryPack;

namespace GameWeb.Application.Maps.Models;

public enum TileType : byte { Empty = 0, Floor = 1, Wall = 2, TreeStump = 3 }

[MemoryPackable]
public partial record MapDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public TileType[] TilesRowMajor { get; init; } = [];
    public byte[] CollisionRowMajor { get; init; } = [];
    public int Width { get; init; }
    public int Height { get; init; }
    public bool UsePadded { get; init; }
    public bool BorderBlocked { get; init; }
}
