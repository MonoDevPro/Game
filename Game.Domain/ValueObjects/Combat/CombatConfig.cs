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
    // Constantes de Regras de Combate/Atributos
    public const int HpPerConstitution = 10;
    public const int HpPerLevel = 5;
    public const int MpPerIntelligence = 5;
    public const int MpPerLevel = 3;
    public const int MinRegenPerTick = 1;
    public const int RegenDivisor = 10;

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
}