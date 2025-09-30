using AutoMapper;
using GameWeb.Domain.Entities;

namespace Application.Abstractions;

public enum Gender : byte
{
    None, 
    Male, 
    Female
}

public enum Vocation : byte
{
    None,
    /// <summary>
    /// Warrior class - melee combat specialist
    /// </summary>
    Warrior,
    /// <summary>
    /// Mage class - magic damage dealer
    /// </summary>
    Mage,
    /// <summary>
    /// Archer class - ranged physical damage dealer
    /// </summary>
    Archer
}

public record PlayerData
{
    public int Id { get; init; }
    public string Name { get; init; } = null!;
    public Gender Gender { get; init; }
    public Vocation Vocation { get; init; }

    public int HealthMax { get; init; }
    public int HealthCurrent { get; init; }
    public int AttackDamage { get; init; }
    public int AttackRange { get; init; }
    public float AttackCastTime { get; init; }
    public float AttackCooldown { get; init; }
    public float MoveSpeed { get; init; }

    // World state (discrete grid)
    public int PosX { get; init; }
    public int PosY { get; init; }

    // Direction vector as integers (e.g. -1/0/1). Keep small for network efficiency.
    public int DirX { get; init; }
    public int DirY { get; init; }

    public class PlayerMappingProfile : Profile
    {
        public PlayerMappingProfile()
        {
            // Entity -> DTO
            CreateMap<Player, PlayerData>()
                // outros campos serão mapeados automaticamente pelo AutoMapper
                .ForMember(d => d.Gender,
                    opt => opt.MapFrom(s => MapGenderFromByte(s.Gender)))
                .ForMember(d => d.Vocation,
                    opt => opt.MapFrom(s => MapVocationFromByte(s.Vocation)));

            // DTO -> Entity
            CreateMap<PlayerData, Player>()
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
}
