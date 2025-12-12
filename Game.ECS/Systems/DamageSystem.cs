using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Events;
using Game.ECS.Schema;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema que aplica dano periódico (DoT) em entidades com Health + DamageOverTime.
/// Usa VitalsLogic.ApplyPeriodicDamage para acumular frações de dano.
/// Também processa ataques melee (imediato) e ranged (cria projétil).
/// </summary>
public sealed partial class DamageSystem(
    World world,
    ILogger<DamageSystem>? logger = null) : GameSystem(world)
{
    [Query]
    [All<Health, DamageOverTime>]
    [None<Invulnerable>]
    private void ProcessDamageOverTime(
        in Entity entity,
        ref Health health,
        ref DamageOverTime dot,
        [Data] float deltaTime)
    {
        // Atualiza tempo restante do efeito
        dot.RemainingTime -= deltaTime;

        // Aplica dano periódico (acumulado)
        bool changed = ApplyPeriodicDamage(
            ref health.Current,
            dot.DamagePerSecond,
            deltaTime,
            ref dot.AccumulatedDamage);

        // Se HP chegou a zero ou o tempo do efeito acabou, remove o DoT
        if (health.Current <= 0 || dot.RemainingTime <= 0f)
            World.Remove<DamageOverTime>(entity);

        if (!changed) 
            return;
        
        var damageEvent = new DamageEvent(Source: entity, Target: entity, Amount: 0, IsCritical: false);
        EventBus.Send(ref damageEvent);
    }
    
    [Query]
    [All<DeferredDamage, Health>]
    [None<Dead, Invulnerable>]
    private void ProcessDeferredDamage(
        in Entity target,
        in DeferredDamage damaged,
        ref Health health)
    {
        // ✅ Aplica o dano
        Damage(damaged.SourceEntity, target, ref health, damaged.Amount, damaged.IsCritical);
        
        World.Remove<DeferredDamage>(target);
    }
    
    public static void Damage(Entity source, Entity target, ref Health health, int amount, bool isCritical = false)
    {
        if (amount <= 0)
            return;
        
        int previous = health.Current;
        int newValue = Math.Max(0, previous - amount);

        if (newValue == previous)
            return;

        health.Current = newValue;
        
        var damageEvent = new DamageEvent(Source: target, Target: target, Amount: amount, IsCritical: isCritical);
    }
    
    public static void ApplyDeferredDamage(World world, in Entity targetEntity, int amount, bool isCritical = false, Entity? attacker = null)
    {
        ref var damaged = ref world.AddOrGet<DeferredDamage>(targetEntity);
        
        damaged.Amount += amount;
        damaged.IsCritical = isCritical;
        damaged.SourceEntity = attacker ?? damaged.SourceEntity;
    }
    
    /// <summary>
    /// Aplica dano periódico baseado em taxa (float) + acumulação.
    /// - acumulated recebe a taxa * deltaTime;
    /// - quando acumulated >= 1, converte para dano inteiro;
    /// - aplica no valor atual, até mínimo 0;
    /// - retorna true se o valor foi alterado.
    /// </summary>
    /// <param name="current">Valor atual (HP, por exemplo).</param>
    /// <param name="damagePerSecond">Taxa de dano por segundo (float).</param>
    /// <param name="deltaTime">Delta de tempo.</param>
    /// <param name="accumulated">Acumulador de frações de dano.</param>
    public static bool ApplyPeriodicDamage(
        ref int current,
        float damagePerSecond,
        float deltaTime,
        ref float accumulated)
    {
        if (current <= 0)
        {
            accumulated = 0f;
            return false;
        }

        if (damagePerSecond <= 0f || deltaTime <= 0f)
            return false;

        accumulated += damagePerSecond * deltaTime;

        if (accumulated < 1.0f)
            return false;

        int damageToApply = (int)accumulated;
        if (damageToApply <= 0)
            return false;

        int previous = current;
        current = Math.Max(0, current - damageToApply);

        accumulated -= damageToApply;

        return current != previous;
    }
}