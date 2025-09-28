namespace Application.Models;

public record CharacterDto(
    int Id,
    string Name,
    string Gender,
    string Vocation,
    bool IsActive,
    int HealthCurrent,
    int HealthMax,
    int AttackDamage,
    int AttackRange,
    float AttackCastTime,
    float AttackCooldown,
    float MoveSpeed,
    int PositionX,
    int PositionY,
    int DirectionX,
    int DirectionY
);
