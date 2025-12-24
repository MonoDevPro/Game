namespace Game.Domain.Combat.ValueObjects;

/// <summary>
/// Configuração de combate específica do servidor para cada entidade.
/// </summary>
public struct CombatConfig
{
    /// <summary>
    /// Duração do ataque em ticks (tempo de animação).
    /// </summary>
    public int AttackDurationTicks;
    
    /// <summary>
    /// Tick em que o dano é aplicado durante o ataque (para sincronização de animação).
    /// </summary>
    public int DamageApplicationTick;
    
    /// <summary>
    /// Se a entidade pode ser interrompida durante ataque.
    /// </summary>
    public bool CanBeInterrupted;

    public static CombatConfig Default => new()
    {
        AttackDurationTicks = 20,
        DamageApplicationTick = 10,
        CanBeInterrupted = true
    };

    public static CombatConfig Knight => new()
    {
        AttackDurationTicks = 25,
        DamageApplicationTick = 15,
        CanBeInterrupted = false  // Knight não é interrompido
    };

    public static CombatConfig Mage => new()
    {
        AttackDurationTicks = 35,
        DamageApplicationTick = 25,  // Dano vem no final do cast
        CanBeInterrupted = true
    };

    public static CombatConfig Archer => new()
    {
        AttackDurationTicks = 15,
        DamageApplicationTick = 10,
        CanBeInterrupted = true
    };
}