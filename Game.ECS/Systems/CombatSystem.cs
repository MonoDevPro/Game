using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável pela lógica de combate: ataque, dano, morte.
/// Processa ataques, aplica dano e gerencia transições para estado Dead.
/// </summary>
public sealed partial class CombatSystem(World world, GameEventSystem events) : GameSystem(world, events)
{
    [Query]
    [All<Health, CombatState>]
    private void ProcessTakeDamage(in Entity e, ref Health health, ref CombatState combat, [Data] float deltaTime)
    {
        if (health.Current <= 0)
        {
            health.Current = 0;
            
            // Marca como morto se ainda não estiver
            if (!World.Has<Dead>(e))
            {
                World.Add<Dead>(e);
                World.MarkNetworkDirty(e, SyncFlags.Vitals);
                Events.RaiseNetworkDirty(e);
                Events.RaiseDeath(e);
            }
        }
    }

    [Query]
    [All<Attackable, CombatState, AttackPower>]
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
    /// Aplica dano a uma entidade alvo.
    /// </summary>
    public bool TryDamage(Entity target, int damage, Entity? attacker = null)
    {
        if (!World.IsAlive(target))
            return false;

        if (!World.TryGet(target, out Health health))
            return false;

        int previous = health.Current;
        int newValue = Math.Max(0, previous - damage);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        World.Set(target, health);
        World.MarkNetworkDirty(target, SyncFlags.Vitals);
        Events.RaiseNetworkDirty(target);

        if (attacker.HasValue)
        {
            Events.RaiseDamage(attacker.Value, target, previous - newValue);
        }
        else
        {
            Events.RaiseDamage(null, target, previous - newValue);
        }

        return true;
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
        World.MarkNetworkDirty(target, SyncFlags.Vitals);
        Events.RaiseNetworkDirty(target);
        Events.RaiseHeal(healer, target, newValue - previous);
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
        World.MarkNetworkDirty(target, SyncFlags.Vitals);
        Events.RaiseNetworkDirty(target);
        Events.RaiseHeal(source, target, newValue - previous);
        return true;
    }
}
