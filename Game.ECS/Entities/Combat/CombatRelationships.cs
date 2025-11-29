using Arch.Core;
using Arch.Relationships;

namespace Game.ECS.Entities.Combat;

public struct TargetOf { }  // "Entity A é alvo de Entity B"
public struct Attacking { } // "Entity A está atacando Entity B"

public static class CombatRelationships
{
    public static void SetCombatTarget(this World world, Entity attacker, Entity target)
    {
        // Adiciona relacionamento: attacker -> target
        world. AddRelationship<Attacking>(attacker, target);
    }
    
    // Consultar todos que estão atacando uma entidade
    public static void ProcessIncomingAttacks(this World world, Entity target)
    {
        ref var attackers = ref world.GetRelationships<Attacking>(target);
        foreach (var relationship in attackers)
        {
            var attacker = relationship.Key;
            // Processar dano do atacante
            
            
            
        }
    }
}