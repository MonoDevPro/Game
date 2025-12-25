using System.Runtime.InteropServices;
using Game.Domain.Enums;

namespace Game.Domain.ValueObjects.Combat;

/// <summary>
/// Configuração de combate específica do servidor para cada entidade.
/// Component ECS para configurar timing e comportamento de ataques.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct CombatConfig
{
    /// <summary>
    /// Duração do ataque em ticks (tempo de animação).
    /// </summary>
    public int AttackDurationTicks { get; init; }
    
    /// <summary>
    /// Tick em que o dano é aplicado durante o ataque (para sincronização de animação).
    /// </summary>
    public int DamageApplicationTick { get; init; }
    
    /// <summary>
    /// Se a entidade pode ser interrompida durante ataque.
    /// </summary>
    public bool CanBeInterrupted { get; init; }

    public CombatConfig(int attackDuration, int damageApplication, bool canInterrupt)
    {
        if (attackDuration <= 0)
            throw new ArgumentException("Attack duration must be positive", nameof(attackDuration));
        if (damageApplication < 0 || damageApplication > attackDuration)
            throw new ArgumentException("Damage application tick must be within attack duration", nameof(damageApplication));

        AttackDurationTicks = attackDuration;
        DamageApplicationTick = damageApplication;
        CanBeInterrupted = canInterrupt;
    }

    /// <summary>
    /// Cria uma configuração baseada na vocação.
    /// </summary>
    public static CombatConfig ForVocation(VocationType vocation) => vocation switch
    {
        VocationType.Knight or VocationType.Berserker => Knight,
        VocationType.Mage or VocationType.Sorcerer or VocationType.Warlock => Mage,
        VocationType.Archer or VocationType.Ranger or VocationType.Assassin => Archer,
        VocationType.Cleric or VocationType.Priest or VocationType.Paladin => Cleric,
        _ => Default
    };

    public static CombatConfig Default => new(20, 10, true);
    public static CombatConfig Knight => new(25, 15, false);
    public static CombatConfig Mage => new(35, 25, true);
    public static CombatConfig Archer => new(15, 10, true);
    public static CombatConfig Cleric => new(30, 20, true);
}
