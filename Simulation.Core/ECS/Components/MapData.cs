using System.Text;
using MemoryPack;

namespace Simulation.Core.ECS.Components; // Mantido o namespace para consistência

public enum TileType : byte { Empty = 0, Floor = 1, Wall = 2, TreeStump = 3 }

[MemoryPackable]
public readonly partial record struct MapData
{
    public int Id { get; init; }
    public string Name { get; init; }
    public TileType[] TilesRowMajor { get; init; }
    public byte[] CollisionRowMajor { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool UsePadded { get; init; }
    public bool BorderBlocked { get; init; }
}
