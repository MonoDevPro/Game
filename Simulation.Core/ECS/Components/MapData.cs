using System.Text;
using MemoryPack;
using Simulation.Core.Persistence.Models;

namespace Simulation.Core.ECS.Components; // Mantido o namespace para consistência

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

    public static MapData FromModel(MapModel model)
    {
        var expectedSize = model.Width * model.Height;

        var tiles = model.TilesRowMajor ?? [];
        var collision = model.CollisionRowMajor ?? [];

        if (tiles.Length != expectedSize)
        {
            tiles = new TileType[expectedSize];
            Array.Fill(tiles, TileType.Floor);
        }

        if (collision.Length != expectedSize)
        {
            collision = new byte[expectedSize];
        }

        return new MapData
        {
            Id = model.Id,
            Name = model.Name ?? string.Empty,
            Width = model.Width,
            Height = model.Height,
            UsePadded = model.UsePadded,
            BorderBlocked = model.BorderBlocked,
            TilesRowMajor = tiles,
            CollisionRowMajor = collision
        };
    }

    public override string ToString()
    {
        var sb = new StringBuilder(256);
        sb.Append("MapData { ");
        sb.Append($"Id = {Id}, ");
        sb.Append($"Name = \"{Name}\", ");
        sb.Append($"Size = {Width}x{Height}, ");
        sb.Append($"UsePadded = {UsePadded}, ");
        sb.Append($"BorderBlocked = {BorderBlocked}, ");

        var tilesLen = TilesRowMajor?.Length ?? 0;
        var collLen = CollisionRowMajor?.Length ?? 0;
        sb.Append($"TilesLength = {tilesLen}, ");
        sb.Append($"CollisionLength = {collLen}");

        // preview dos primeiros tiles (até 8)
        if (tilesLen > 0)
        {
            if (TilesRowMajor != null)
            {
                var preview = string.Join(", ", TilesRowMajor.Take(Math.Min(8, tilesLen)).Select(t => t.ToString()));
                sb.Append($", TilesPreview = [{preview}");
            }

            if (tilesLen > 8) sb.Append(", ...");
            sb.Append("]");
        }

        // resumo de colisões (número de células bloqueadas)
        if (collLen > 0)
        {
            if (CollisionRowMajor != null)
            {
                int blocked = CollisionRowMajor.Count(b => b != 0);
                sb.Append($", BlockedCells = {blocked}");
            }
        }

        sb.Append(" }");
        return sb.ToString();
    }
}
