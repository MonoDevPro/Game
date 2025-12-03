using Game.Domain.Enums;
using Game.ECS.Schema.Components;

namespace Game.ECS.Entities.Combat;

public static class CombatHelper
{
    /// <summary>
    /// Retorna o estilo de ataque básico baseado no ID de vocação (byte).
    /// </summary>
    public static AttackStyle GetAttackStyleForVocation(byte vocationId) 
        => GetAttackStyleForVocation((VocationType)vocationId);
    
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
}