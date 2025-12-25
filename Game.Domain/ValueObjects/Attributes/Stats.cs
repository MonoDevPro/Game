namespace Game.Domain.ValueObjects.Attributes;

/// <summary>
/// Stats de combate da entidade.
/// Component ECS para representar todos os atributos derivados de combate.
/// </summary>
public readonly record struct Stats(
    double PhysicalAttack,
    double MagicAttack,
    double PhysicalDefense,
    double MagicDefense,
    double AttackRange,
    double AttackSpeed,
    double MovementSpeed,
    double CriticalChance);