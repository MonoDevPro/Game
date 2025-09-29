using Application.Models;
using Application.Models.Models;
using GameWeb.Domain.Entities;

namespace GameWeb.Application.Characters.Models;

public class CharacterMappingProfile : Profile
{
    public CharacterMappingProfile()
    {
        CreateMap<Character, CharacterDto>()
            .ForCtorParam("Id", opt => opt.MapFrom(src => src.Id))
            .ForCtorParam("Name", opt => opt.MapFrom(src => src.Name))
            .ForCtorParam("Gender", opt => opt.MapFrom(src => Enum.GetName(src.Gender)))
            .ForCtorParam("Vocation", opt => opt.MapFrom(src => Enum.GetName(src.Vocation)))
            .ForCtorParam("IsActive", opt => opt.MapFrom(src => src.IsActive))
            .ForCtorParam("HealthCurrent", opt => opt.MapFrom(src => src.Stats.HealthCurrent))
            .ForCtorParam("HealthMax", opt => opt.MapFrom(src => src.Stats.HealthMax))
            .ForCtorParam("AttackDamage", opt => opt.MapFrom(src => src.Stats.AttackDamage))
            .ForCtorParam("AttackRange", opt => opt.MapFrom(src => src.Stats.AttackRange))
            .ForCtorParam("AttackCastTime", opt => opt.MapFrom(src => src.Stats.AttackCastTime))
            .ForCtorParam("AttackCooldown", opt => opt.MapFrom(src => src.Stats.AttackCooldown))
            .ForCtorParam("MoveSpeed", opt => opt.MapFrom(src => src.Stats.MoveSpeed))
            .ForCtorParam("PositionX", opt => opt.MapFrom(src => src.Position.X))
            .ForCtorParam("PositionY", opt => opt.MapFrom(src => src.Position.Y))
            .ForCtorParam("DirectionX", opt => opt.MapFrom(src => src.Direction.X))
            .ForCtorParam("DirectionY", opt => opt.MapFrom(src => src.Direction.Y));
    }
}
