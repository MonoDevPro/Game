namespace Simulation.Core.Persistence.Models;

public enum TileType : byte { Empty = 0, Floor = 1, Wall = 2, TreeStump = 3 }

public class MapModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    // row-major arrays: length = Width * Height
    public TileType[]? TilesRowMajor { get; set; }
    public byte[]? CollisionRowMajor { get; set; }
    // ECS Identifiers
    public int Width { get; set; }
    public int Height { get; set; }
    public bool UsePadded { get; set; } = false;
    public bool BorderBlocked { get; set; } = true;
}