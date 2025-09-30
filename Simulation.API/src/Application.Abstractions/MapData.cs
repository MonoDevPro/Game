using AutoMapper;
using GameWeb.Domain.Entities;
using MemoryPack;

namespace Application.Abstractions;

public enum TileType : byte { Empty = 0, Floor = 1, Wall = 2, TreeStump = 3 }
[MemoryPackable]
public partial record MapData
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public TileType[] TilesRowMajor { get; init; } = [];
    public byte[] CollisionRowMajor { get; init; } = [];
    public int Width { get; init; }
    public int Height { get; init; }
    public bool UsePadded { get; init; }
    public bool BorderBlocked { get; init; }
    
    public class MapMappingProfile : Profile
    {
        public MapMappingProfile()
        {
            // Map entity -> DTO
            CreateMap<Map, MapData>()
                .ForMember(d => d.TilesRowMajor,
                    opt => 
                        opt.MapFrom(s => (s.Tiles).Select(b => (TileType)b).ToArray()));

            // Map DTO -> entity
            CreateMap<MapData, Map>()
                .ForMember(d => d.Tiles,
                    opt => opt.MapFrom(s =>
                        (s.TilesRowMajor).Select(t => (byte)t).ToArray()));
        }
    }
}
