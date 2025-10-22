using System;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável pela lógica de combate: ataque, dano, morte.
/// Processa ataques, aplica dano e gerencia transições para estado Dead.
/// </summary>
public sealed partial class CombatSystem(World world, GameEventSystem events, EntityFactory factory) 
    : GameSystem(world, events, factory)
{
    [Query]
    [All<Health, CombatState>]
    [None<Dead>]
    private void ProcessTakeDamage(in Entity e, ref Health health, ref CombatState combat, [Data] float deltaTime)
    {
        if (health.Current <= 0)
        {
            health.Current = 0;
            
            // Marca como morto se ainda não estiver
            if (!World.Has<Dead>(e))
            {
                World.Add<Dead>(e);
                Events.RaiseDeath(e);
            }
        }
    }

    [Query]
    [All<Attackable, CombatState, AttackPower>]
    [None<Dead>]
    private void ProcessAttackCooldown(in Entity e, ref Attackable attackable, ref CombatState combat, [Data] float deltaTime)
    {
        if (combat.LastAttackTime > 0)
        {
            combat.LastAttackTime -= deltaTime;
        }
    }

    /// <summary>
    /// Calcula o dano total considerando ataque físico/mágico e defesa da vítima.
    /// </summary>
    public static int CalculateDamage(in AttackPower attack, in Defense defense, bool isMagical = false)
    {
        int attackPower = isMagical ? attack.Magical : attack.Physical;
        int defensePower = isMagical ? defense.Magical : defense.Physical;
        
        // Defesa reduz dano: max(1, ataque - defesa)
        int baseDamage = Math.Max(1, attackPower - defensePower);
        
        // Variação aleatória: ±20%
        float variance = 0.8f + (float)Random.Shared.NextDouble() * 0.4f;
        return (int)(baseDamage * variance);
    }

    /// <summary>
    /// Realiza um ataque corpo-a-corpo com validações de cooldown e alcance.
    /// </summary>
    public bool TryAttack(Entity attacker, Entity target)
    {
        if (!World.IsAlive(attacker) || !World.IsAlive(target))
            return false;

        if (!World.TryGet(attacker, out Position attackerPos) ||
            !World.TryGet(target, out Position targetPos) ||
            !World.TryGet(attacker, out AttackPower attackPower) ||
            !World.TryGet(target, out Defense defense) ||
            !World.TryGet(attacker, out CombatState combat) ||
            !World.TryGet(attacker, out Attackable attackable))
            return false;

        if (combat.LastAttackTime > 0f)
            return false;

        int distance = attackerPos.ManhattanDistance(targetPos);
        if (distance > SimulationConfig.MaxMeleeAttackRange)
            return false;

        if (World.Has<Dead>(target) || World.Has<Invulnerable>(target))
            return false;

    float baseSpeed = Math.Max(0.1f, attackable.BaseSpeed);
    float modifier = Math.Max(0.1f, attackable.CurrentModifier);
    float attacksPerSecond = baseSpeed * modifier;
    combat.LastAttackTime = 1f / attacksPerSecond;
        bool wasInCombat = combat.InCombat;
        combat.InCombat = true;
        combat.TargetNetworkId = World.TryGet(target, out NetworkId netId) ? netId.Value : 0;
        World.Set(attacker, combat);

        if (World.Has<DirtyFlags>(attacker))
        {
            ref DirtyFlags attackerDirty = ref World.Get<DirtyFlags>(attacker);
            attackerDirty.MarkDirty(DirtyComponentType.Combat);
        }

        int damage = CalculateDamage(attackPower, defense);
        if (ApplyDamageInternal(target, damage, attacker))
        {
            if (!wasInCombat)
                Events.RaiseCombatEnter(attacker);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Aplica dano a uma entidade alvo.
    /// </summary>
    public bool TryDamage(Entity target, int damage, Entity? attacker = null)
    {
        if (!World.IsAlive(target) || !World.Has<Health>(target))
            return false;

        if (damage <= 0)
            return false;

        return ApplyDamageInternal(target, damage, attacker);
    }

    /// <summary>
    /// Restaura vida de uma entidade (para poções, curas, etc).
    /// </summary>
    public bool TryHeal(Entity target, int amount, Entity? healer = null)
    {
        if (!World.IsAlive(target))
            return false;

        if (!World.TryGet(target, out Health health))
            return false;

        int previous = health.Current;
        int newValue = Math.Min(health.Max, previous + amount);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        World.Set(target, health);

        if (World.Has<DirtyFlags>(target))
        {
            ref DirtyFlags dirty = ref World.Get<DirtyFlags>(target);
            dirty.MarkDirty(DirtyComponentType.Health);
        }

        Events.RaiseHealHp(healer, target, newValue - previous);
        return true;
    }

    /// <summary>
    /// Restaura mana de uma entidade.
    /// </summary>
    public bool TryRestoreMana(Entity target, int amount, Entity? source = null)
    {
        if (!World.IsAlive(target))
            return false;

        if (!World.TryGet(target, out Mana mana))
            return false;

        int previous = mana.Current;
        int newValue = Math.Min(mana.Max, previous + amount);

        if (newValue == previous)
            return false;

        mana.Current = newValue;
        World.Set(target, mana);

        if (World.Has<DirtyFlags>(target))
        {
            ref DirtyFlags dirty = ref World.Get<DirtyFlags>(target);
            dirty.MarkDirty(DirtyComponentType.Mana);
        }

        Events.RaiseHealMp(source, target, newValue - previous);
        return true;
    }

    private bool ApplyDamageInternal(Entity target, int damage, Entity? attacker)
    {
        ref Health health = ref World.Get<Health>(target);
        int previous = health.Current;
        int newValue = Math.Max(0, previous - damage);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        World.Set(target, health);

        if (World.Has<DirtyFlags>(target))
        {
            ref DirtyFlags dirty = ref World.Get<DirtyFlags>(target);
            dirty.MarkDirty(DirtyComponentType.Health);
        }

        Events.RaiseDamage(attacker, target, previous - newValue);

        if (health.Current <= 0 && !World.Has<Dead>(target))
        {
            World.Add<Dead>(target);
            Events.RaiseDeath(target, attacker);
        }

        return true;
    }
}
