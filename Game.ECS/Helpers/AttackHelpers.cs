using System.Runtime.CompilerServices;
using Game.Domain.Enums;
using Game.DTOs.Game.Player;
using Game.ECS.Components;

namespace Game.ECS.Helpers;

public class AttackHelpers
{
    // Attack range by style
    public const float MeleeRange = 1.5f;
    public const float RangedRange = 8f;
    public const float MagicRange = 10f;
    
    /// <summary>
    /// Gets attack style based on vocation ID.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AttackStyle GetAttackStyleFromVocation(byte voc)
    {
        return (VocationType)voc switch
        {
            VocationType.Warrior => AttackStyle.Melee,   // Warrior
            VocationType.Archer => AttackStyle.Ranged,  // Archer
            VocationType.Mage => AttackStyle.Magic,   // Mage
            _ => AttackStyle.Melee    // Default
        };
    }
    /// <summary>
    /// Determina o estilo de ataque baseado no comportamento e stats.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static AttackStyle DetermineAttackStyle(in CombatStats stats)
    {
        // Se o range de ataque é maior que 1. 5, usa ataque à distância
        if (stats. AttackRange > 1.5f)
        {
            // Se tem mais poder mágico, usa magia
            return stats.MagicPower > stats.AttackPower 
                ? AttackStyle.Magic 
                : AttackStyle. Ranged;
        }
        
        return AttackStyle.Melee;
    }
    
    /// <summary>
    /// Retorna o tempo base de conjuração por estilo de ataque.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetBaseConjureTime(AttackStyle style)
    {
        return style switch
        {
            AttackStyle. Melee => 0.2f,   // Rápido - só "armar" o golpe
            AttackStyle. Ranged => 0.4f,  // Médio - mirar e preparar
            AttackStyle.Magic => 0.8f,   // Lento - canalizar magia
            _ => 0.3f
        };
    }

    /// <summary>
    /// Gets attack range based on attack style.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float GetAttackRange(AttackStyle style) => style switch
    {
        AttackStyle.Melee => MeleeRange,
        AttackStyle.Ranged => RangedRange,
        AttackStyle.Magic => MagicRange,
        _ => MeleeRange
    };
}