using Game.Domain.Enums;

namespace Game.Domain.Entities;

public class Map : BaseEntity
{
    public string Name { get; init; } = null!;
    public int Width { get; set; }
    public int Height { get; set; }
    public int Count => Width * Height;
    public bool UsePadded { get; set; } = false;
    public int PaddedSize { get; set; } = 0; // p (if padded)
    public bool BorderBlocked { get; init; }
    public TileType[] Tiles { get; set; } = [];
    public byte[] CollisionMask { get; set; } = []; // 0=free, 1=blocked
}
