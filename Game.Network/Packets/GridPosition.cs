using Game.Domain.VOs;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Simple grid position payload for network messages (X,Y in tile coordinates).
/// </summary>
[MemoryPackable]
public partial struct GridPosition
{
    public int X { get; set; }
    public int Y { get; set; }

    public GridPosition(int x, int y)
    {
        X = x;
        Y = y;
    }

    public static GridPosition FromCoordinate(Coordinate coordinate) => new(coordinate.X, coordinate.Y);

    public Coordinate ToCoordinate() => new(X, Y);
}
