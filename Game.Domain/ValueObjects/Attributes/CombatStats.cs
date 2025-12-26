namespace Game.Domain.ValueObjects.Attributes;

/// <summary>
/// Stats de combate da entidade.
/// Component ECS para representar todos os atributos derivados de combate.
/// </summary>
public readonly record struct CombatStats(
    double PhysicalAttack,
    double MagicAttack,
    double PhysicalDefense,
    double MagicDefense)
{
    public static CombatStats Zero => default;
    
    public static CombatStats operator +(CombatStats a, CombatStats b) => new(
        a.PhysicalAttack + b.PhysicalAttack,
        a.MagicAttack + b.MagicAttack,
        a.PhysicalDefense + b.PhysicalDefense,
        a.MagicDefense + b.MagicDefense);
    
    public static CombatStats operator *(CombatStats combatStats, double factor) => new(
        combatStats.PhysicalAttack * factor,
        combatStats.MagicAttack * factor,
        combatStats.PhysicalDefense * factor,
        combatStats.MagicDefense * factor);
    
    public static CombatStats operator *(CombatStats combatStats, CombatStats factor) => new(
        combatStats.PhysicalAttack * factor.PhysicalAttack,
        combatStats.MagicAttack * factor.MagicAttack,
        combatStats.PhysicalDefense * factor.PhysicalDefense,
        combatStats.MagicDefense * factor.MagicDefense);
    
}