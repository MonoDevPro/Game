using Game.Domain.Enums;

namespace Game.Domain.Attributes.Vocation.ValueObjects;

/// <summary>
/// Perfil de combate específico da vocação.
/// </summary>
public readonly record struct VocationCombatProfile(
    int BaseAttackRange,
    float BaseAttackSpeed,
    float BaseCriticalChance,
    float BaseCriticalDamage,
    int ManaCostPerAttack,
    DamageType PrimaryDamageType)
{
    public static VocationCombatProfile Melee => new(1, 1.0f, 5f, 150f, 0, DamageType.Physical);
    public static VocationCombatProfile Ranged => new(8, 1.2f, 10f, 175f, 0, DamageType.Physical);
    public static VocationCombatProfile Magic => new(6, 0.8f, 8f, 150f, 10, DamageType.Magical);
    public static VocationCombatProfile Hybrid => new(1, 1.0f, 6f, 150f, 5, DamageType.Physical);
}