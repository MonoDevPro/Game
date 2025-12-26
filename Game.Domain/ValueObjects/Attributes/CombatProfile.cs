using Game.Domain.Enums;

namespace Game.Domain.ValueObjects.Attributes;

public readonly record struct CombatProfile(
    int AttackRange,
    double AttackSpeed,
    double CriticalChance,
    double CriticalDamage,
    double ManaCostPerAttack,
    DamageType DamageType)
{
    public static CombatProfile Melee => new(1, 1.0f, 5f, 150f, 0, DamageType.Physical);
    public static CombatProfile Ranged => new(8, 1.2f, 10f, 175f, 0, DamageType.Physical);
    public static CombatProfile Magic => new(6, 0.8f, 8f, 150f, 10, DamageType.Magical);
    public static CombatProfile Hybrid => new(1, 1.0f, 6f, 150f, 5, DamageType.Physical | DamageType.Magical);
}

