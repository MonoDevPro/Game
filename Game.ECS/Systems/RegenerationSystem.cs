using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Entities.Components;
using Game.ECS.Events;
using Game.ECS.Schema;
using Game.ECS.Schema.Components;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por regeneração de vida e mana.
/// Processa entidades que têm Health e Mana, aplicando regeneração por tick.
/// </summary>
public sealed partial class RegenerationSystem(World world, ILogger<RegenerationSystem>? logger = null) : GameSystem(world)
{
    [Query]
    [All<Health>]
    [None<Dead>]
    private void ProcessHealthRegeneration(
        in Entity entity,
        ref Health health,
        [Data] float deltaTime)
    {
        var oldHealth = health.Current;
        
        // Usa lógica unificada de regen acumulada
        bool changed = ApplyRegeneration(
            ref health.Current,
            health.Max,
            health.RegenerationRate,
            deltaTime,
            ref health.AccumulatedRegeneration);

        if (changed)
        {
            var healthEvent = new HealthChangedEvent(entity, oldHealth, health.Current, health.Max);
            EventBus.Send(ref healthEvent);
        }
    }

    [Query]
    [All<Mana>]
    [None<Dead>]
    private void ProcessManaRegeneration(
        in Entity entity,
        ref Mana mana,
        [Data] float deltaTime)
    {
        var oldMana = mana.Current;

        bool changed = ApplyRegeneration(
            ref mana.Current,
            mana.Max,
            mana.RegenerationRate,
            deltaTime,
            ref mana.AccumulatedRegeneration);
        
        if (changed)
        {
            var manaEvent = new ManaChangedEvent(entity, oldMana, mana.Current, mana.Max);
            EventBus.Send(ref manaEvent);
        }
    }
    
    /// <summary>
    /// Lida com regeneração baseada em taxa float + acumulação, aplicando
    /// apenas os pontos inteiros quando acumulados.
    /// Exemplos de uso:
    /// - HP: RegenRate = 0.7/s → acumula até >= 1, aplica 1, sobra fração.
    /// - MP: mesmo padrão.
    /// </summary>
    /// <param name="current">valor atual (HP/MP/etc).</param>
    /// <param name="max">valor máximo.</param>
    /// <param name="regenRatePerSecond">taxa de regen por segundo (float).</param>
    /// <param name="deltaTime">delta de tempo em segundos.</param>
    /// <param name="accumulated">
    /// acumulador de frações; será incrementado e terá os inteiros descontados.
    /// </param>
    /// <returns>true se o valor atual foi incrementado.</returns>
    public static bool ApplyRegeneration(
        ref int current,
        int max,
        float regenRatePerSecond,
        float deltaTime,
        ref float accumulated)
    {
        // Já está cheio, zera acumulação para não vlogar float ao infinito.
        if (current >= max)
        {
            accumulated = 0f;
            return false;
        }

        if (regenRatePerSecond <= 0f || deltaTime <= 0f)
            return false;

        accumulated += regenRatePerSecond * deltaTime;

        if (accumulated < 1.0f)
            return false;

        int regenToApply = (int)accumulated;
        if (regenToApply <= 0)
            return false;

        int previous = current;
        current = Math.Min(max, current + regenToApply);

        // Desconta apenas o que virou inteiro.
        accumulated -= regenToApply;

        return current != previous;
    }
    
    public static bool TryHeal(ref Health health, int amount)
    {
        if (amount <= 0)
            return false;
        
        int previous = health.Current;
        int newValue = Math.Min(health.Max, previous + amount);

        if (newValue == previous)
            return false;

        health.Current = newValue;
        return true;
    }

    public static bool TryRestoreMana(ref Mana mana, int amount)
    {
        if (amount <= 0)
            return false;

        int previous = mana.Current;
        int newValue = Math.Min(mana.Max, previous + amount);

        if (newValue == previous)
            return false;

        mana.Current = newValue;
        return true;
    }
    
}