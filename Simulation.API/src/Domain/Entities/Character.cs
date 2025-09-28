namespace GameWeb.Domain.Entities;

public record CharacterStats(
    int Id,
    int HealthMax,
    int HealthCurrent,
    int AttackDamage,
    int AttackRange,
    float AttackCastTime,
    float AttackCooldown,
    float MoveSpeed);

public class Character : BaseAuditableEntity
{
    public string Name { get; init ; }
    public Gender Gender { get; init; }
    public Vocation Vocation { get; init; }

    public CharacterStats Stats { get; set; }
    public Vector2D Position { get; set; }
    public Vector2D Direction { get; set; }
    
    public string OwnerId { get; init; } = null!;

#pragma warning disable CS8618
    public Character() { }
#pragma warning restore CS8618
}
