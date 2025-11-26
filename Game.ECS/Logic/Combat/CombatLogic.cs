using Arch.Core;
using Game.Domain.Enums;
using Game.ECS.Components;

namespace Game.ECS.Logic;

public static partial class CombatLogic
{
    /// <summary>
    /// Retorna o estilo de ataque básico baseado na vocação.
    /// </summary>
    public static AttackStyle GetAttackStyleForVocation(VocationType vocation) => vocation switch
    {
        VocationType.Warrior => AttackStyle.Melee,
        VocationType.Archer  => AttackStyle.Ranged,
        VocationType.Mage    => AttackStyle.Magic,
        _ => AttackStyle.Melee // Default para vocações desconhecidas
    };
    
    /// <summary>
    /// Retorna o estilo de ataque básico baseado no ID de vocação (byte).
    /// </summary>
    public static AttackStyle GetAttackStyleForVocation(byte vocationId) 
        => GetAttackStyleForVocation((VocationType)vocationId);
    
}