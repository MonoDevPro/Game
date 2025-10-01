using GameWeb.Application.Players.Models;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Common.Mapping.Profiles;

public class PlayerMappingProfile : Profile
{
    public PlayerMappingProfile()
    {
        // Entity -> DTO
        CreateMap<Player, PlayerDto>()
            // outros campos serão mapeados automaticamente pelo AutoMapper
            .ForMember(d => d.Gender,
                opt => opt.MapFrom(s => MapGenderFromByte(s.Gender)))
            .ForMember(d => d.Vocation,
                opt => opt.MapFrom(s => MapVocationFromByte(s.Vocation)));

        // DTO -> Entity
        CreateMap<PlayerDto, Player>()
            // garantir que o byte seja corretamente armazenado
            .ForMember(d => d.Gender,
                opt => opt.MapFrom(s => (byte)s.Gender))
            .ForMember(d => d.Vocation,
                opt => opt.MapFrom(s => (byte)s.Vocation))
            // se quiser evitar sobrescrever UserId/Id/Auditoria em maps parciais, configure As needed
            ;
    }
        
    private static Gender MapGenderFromByte(byte b)
    {
        // mapear valores inválidos para None para evitar casting exception / garbage
        return Enum.IsDefined(typeof(Gender), (int)b)
            ? (Gender)b
            : Gender.None;
    }

    private static Vocation MapVocationFromByte(byte b)
    {
        return Enum.IsDefined(typeof(Vocation), (int)b)
            ? (Vocation)b
            : Vocation.None;
    }
}
