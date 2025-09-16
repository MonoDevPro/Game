using MemoryPack;
using Simulation.Core.Persistence.Models;

namespace Simulation.Core.ECS.Staging.Map; // Mantido o namespace para consistência

/// <summary>
/// Um componente que transporta os dados brutos de um mapa, geralmente
/// carregados da base de dados, para dentro do mundo ECS.
/// Sendo um 'record struct', é um value-type imutável e performático.
/// </summary>
[MemoryPackable]
public readonly partial record struct MapData
{
    public string Name { get; init; }
    public TileType[] TilesRowMajor { get; init; }
    public byte[] CollisionRowMajor { get; init; }
    public int MapId { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
    public bool UsePadded { get; init; }
    public bool BorderBlocked { get; init; }

    /// <summary>
    /// Construtor de fábrica para criar um MapData a partir de um MapModel.
    /// Garante que os arrays nunca sejam nulos.
    /// </summary>
    public static MapData FromModel(MapModel model)
    {
        var expectedSize = model.Width * model.Height;

        // Garante que os arrays não são nulos, criando arrays vazios como fallback.
        var tiles = model.TilesRowMajor ??= [];
        var collision = model.CollisionRowMajor ?? [];

        // Validação de segurança para evitar erros em runtime.
        if (tiles.Length != expectedSize)
        {
            // Logar um aviso aqui seria ideal.
            tiles = new TileType[expectedSize];
            Array.Fill(tiles, TileType.Floor); // Preenche com um padrão seguro.
        }

        if (collision.Length != expectedSize)
        {
            // Logar um aviso aqui.
            collision = new byte[expectedSize];
        }

        return new MapData
        {
            MapId = model.MapId,
            Name = model.Name ?? string.Empty,
            Width = model.Width,
            Height = model.Height,
            UsePadded = model.UsePadded,
            BorderBlocked = model.BorderBlocked,
            TilesRowMajor = tiles,
            CollisionRowMajor = collision
        };
    }
}