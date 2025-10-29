using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por regeneração de vida e mana.
/// Processa entidades que têm Health e Mana, aplicando regeneração por tick.
/// </summary>
public sealed partial class HealthSystem(World world, GameEventSystem eventSystem)
    : GameSystem(world, eventSystem)
{
    [Query]
    [All<Health, DirtyFlags>]
    [None<Dead>]
    private void ProcessHealthRegeneration(in Entity e, ref Health health, ref DirtyFlags dirty, [Data] float deltaTime)
    {
        if (health.Current >= health.Max)
            return;

        float regeneration = health.RegenerationRate * deltaTime;
        int previous = health.Current;
        int regenerated = Math.Min(health.Max, previous + (int)regeneration);

        if (regenerated == previous)
            return;

        health.Current = regenerated;
        dirty.MarkDirty(DirtyComponentType.Health);
    }

    [Query]
    [All<Mana, DirtyFlags>]
    [None<Dead>]
    private void ProcessManaRegeneration(in Entity e, ref Mana mana, ref DirtyFlags dirty, [Data] float deltaTime)
    {
        if (mana.Current >= mana.Max)
            return;

        float regeneration = mana.RegenerationRate * deltaTime;
        int previous = mana.Current;
        int regenerated = Math.Min(mana.Max, previous + (int)regeneration);

        if (regenerated == previous)
            return;

        mana.Current = regenerated;
        dirty.MarkDirty(DirtyComponentType.Mana);
    }

    [Query]
    [All<Dead>]
    private void ProcessDeadEntities(in Entity e, ref CombatState combat, [Data] float deltaTime)
    {
        // Aqui pode haver lógica adicional para entidades mortas
        // Por exemplo: ragdoll timeout, respawn timer, etc
    }
}
