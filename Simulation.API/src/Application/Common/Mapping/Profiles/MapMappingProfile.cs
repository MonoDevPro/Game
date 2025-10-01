using GameWeb.Application.Maps.Models;
using GameWeb.Domain.Entities;
using MapDto = GameWeb.Application.Maps.Models.MapDto;

namespace GameWeb.Application.Common.Mapping.Profiles;

public class MapMappingProfile : Profile
{
    public MapMappingProfile()
    {
        // Map entity -> DTO
        CreateMap<Map, MapDto>()
            .ForMember(d => d.TilesRowMajor,
                opt => 
                    opt.MapFrom(s => (s.Tiles).Select(b => (TileType)b).ToArray()));

        // Map DTO -> entity
        CreateMap<MapDto, Map>()
            .ForMember(d => d.Tiles,
                opt => opt.MapFrom(s =>
                    (s.TilesRowMajor).Select(t => (byte)t).ToArray()));
    }
}
