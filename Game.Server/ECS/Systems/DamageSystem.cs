using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Events;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema que aplica dano periódico (DoT) em entidades com Health + DamageOverTime.
/// Usa VitalsLogic.ApplyPeriodicDamage para acumular frações de dano.
/// Também processa ataques melee (imediato) e ranged (cria projétil).
/// </summary>
public sealed partial class DamageSystem(
    World world,
    IMapService mapService,
    ILogger<DamageSystem>? logger = null
    ) : GameSystem(world)
{
    [Query]
    [All<Health, DamageOverTime, DirtyFlags>]
    [None<Dead, Invulnerable>]
    private void ProcessDamageOverTime(
        in Entity entity,
        ref Health health,
        ref DamageOverTime dot,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        // Atualiza tempo restante do efeito
        dot.RemainingTime -= deltaTime;

        // Aplica dano periódico (acumulado)
        bool changed = DamageLogic.ApplyPeriodicDamage(
            ref health.Current,
            dot.DamagePerSecond,
            deltaTime,
            ref dot.AccumulatedDamage);

        // Se HP chegou a zero ou o tempo do efeito acabou, remove o DoT
        if (health.Current <= 0 || dot.RemainingTime <= 0f)
            World.Remove<DamageOverTime>(entity);
    }
    
    [Query]
    [All<Damaged, Health>]
    [None<Dead, Invulnerable>]
    private void ProcessDeferredDamage(
        in Entity target,
        in Damaged damaged,
        ref Health health,
        ref DirtyFlags dirty)
    {
        // ✅ Aplica o dano
        Damage(damaged.SourceEntity, target, ref health, damaged.Amount, damaged.IsCritical);
        
        World.Remove<Damaged>(target);
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
        EventBus
    }
}