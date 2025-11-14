using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities.Repositories;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por aplicar dano de forma diferida durante a animação de ataque.
/// O dano é aplicado em um dos 3 momentos da animação (Early, Mid, Late) baseado no tipo de ataque.
/// </summary>
public sealed partial class DeferredDamageSystem(World world, PlayerIndex playerIndex, ILogger<DeferredDamageSystem>? logger = null) 
    : GameSystem(world)
{
    [Query]
    [All<Damaged, Health>]
    [None<Dead, Invulnerable>]
    private void ProcessDamage(
        in Entity victim,
        in Damaged damaged,
        ref DirtyFlags dirty,
        [Data] float _)
    {
        if (CombatLogic.TryDamage(World, victim, damaged.Amount))
        {
            // Marca o alvo como dirty para enviar vitals atualizados
            dirty.MarkDirty(DirtyComponentType.Health);

            logger?.LogDebug(
                "Dano imediato aplicado: {Damage} para {Target} de {Attacker}",
                damaged.Amount,
                World.TryGet(victim, out NetworkId targetNetId) ? targetNetId.Value : -1,
                World.TryGet(damaged.SourceEntity, out NetworkId attackerNetId) ? attackerNetId.Value : -1);
        }
    }
}
