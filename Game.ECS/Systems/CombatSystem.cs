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
public sealed partial class CombatSystem(World world) : GameSystem(world)
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
    public bool TryDamage(Entity target, int damage)
    {
        if (!World.IsAlive(target))
            return false;

        if (!World.TryGet(target, out Health health))
            return false;

        health.Current = Math.Max(0, health.Current - damage);
        World.Set(target, health);
        World.MarkNetworkDirty(target, SyncFlags.Vitals);

        return true;
    }

    /// <summary>
    /// Restaura vida ou mana de uma entidade (para poções, curas, etc).
    /// </summary>
    public bool TryHeal(Entity target, int amount, bool isHeal = true)
    {
        if (!World.IsAlive(target))
            return false;

        if (isHeal && World.TryGet(target, out Health health))
        {
            health.Current = Math.Min(health.Current + amount, health.Max);
            World.Set(target, health);
            World.MarkNetworkDirty(target, SyncFlags.Vitals);
            return true;
        }

        if (!isHeal && World.TryGet(target, out Mana mana))
        {
            mana.Current = Math.Min(mana.Current + amount, mana.Max);
            World.Set(target, mana);
            World.MarkNetworkDirty(target, SyncFlags.Vitals);
            return true;
        }

        return false;
    }
}
