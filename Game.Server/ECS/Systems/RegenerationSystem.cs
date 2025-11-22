using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por regeneração de vida e mana.
/// Processa entidades que têm Health e Mana, aplicando regeneração por tick.
/// </summary>
public sealed partial class RegenerationSystem(World world, ILogger<RegenerationSystem>? logger = null) : GameSystem(world)
{
    [Query]
    [All<Health, DirtyFlags, CombatState>]
    [None<Dead>]
    private void ProcessHealthRegeneration(
        ref Health health,
        ref DirtyFlags dirty,
        in CombatState combat,
        [Data] float deltaTime)
    {
        if (combat.InCombat)
            return;

        // Usa lógica unificada de regen acumulada
        bool changed = RegenLogic.ApplyRegeneration(
            ref health.Current,
            health.Max,
            health.RegenerationRate,
            deltaTime,
            ref health.AccumulatedRegeneration);

        if (changed)
            dirty.MarkDirty(DirtyComponentType.Vitals);
    }

    [Query]
    [All<Mana, DirtyFlags, CombatState>]
    [None<Dead>]
    private void ProcessManaRegeneration(
        ref Mana mana,
        ref DirtyFlags dirty,
        in CombatState combat,
        [Data] float deltaTime)
    {
        if (combat.InCombat)
            return;

        bool changed = RegenLogic.ApplyRegeneration(
            ref mana.Current,
            mana.Max,
            mana.RegenerationRate,
            deltaTime,
            ref mana.AccumulatedRegeneration);
        
        if (changed)
            dirty.MarkDirty(DirtyComponentType.Vitals);
    }
}