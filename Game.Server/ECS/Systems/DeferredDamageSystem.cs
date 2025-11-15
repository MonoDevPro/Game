using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por aplicar dano de forma diferida durante a animação de ataque.
/// O dano é aplicado em um dos 3 momentos da animação (Early, Mid, Late) baseado no tipo de ataque.
/// </summary>
public sealed partial class DeferredDamageSystem(
    World world, 
    ILogger<DeferredDamageSystem>? logger = null) 
    : GameSystem(world)
{
    [Query]
    [All<Damaged, Health>]
    [None<Dead, Invulnerable>]
    private void ProcessDamage(
        in Entity victim,
        in Damaged damaged,
        ref Health health,
        ref DirtyFlags dirty)
    {
        var damage = damaged.Amount;
        if (damaged.Amount <= 0)
        {
            World.Remove<Damaged>(victim);
            return;
        }
        
        int newHealth = Math.Max(0, health.Current - damage);
        if (newHealth != health.Current)
            dirty.MarkDirty(DirtyComponentType.Health | DirtyComponentType.Damaged);
        
        health.Current = newHealth;
        if (health.Current <= 0)
            World.Add<Dead>(victim);
        
        World.Remove<Damaged>(victim);
        
        logger?.LogDebug(
            "Dano imediato aplicado: {Damage} para {Target} de {Attacker}",
            damaged.Amount,
            World.TryGet(victim, out NetworkId targetNetId) ? targetNetId.Value : -1,
            World.TryGet(damaged.SourceEntity, out NetworkId attackerNetId) ? attackerNetId.Value : -1);
    }
}