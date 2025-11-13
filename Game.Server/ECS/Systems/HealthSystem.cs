using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por regeneração de vida e mana.
/// Processa entidades que têm Health e Mana, aplicando regeneração por tick.
/// </summary>
public sealed partial class HealthSystem(World world, ILogger<HealthSystem>? logger = null)
    : GameSystem(world)
{
    [Query]
    [All<Health, DirtyFlags>]
    [None<Dead>]
    private void ProcessHealthRegeneration(in Entity e, ref Health health, ref DirtyFlags dirty, [Data] float deltaTime)
    {
        if (health.Current >= health.Max)
        {
            health.AccumulatedRegeneration = 0f;
            return;
        }

        health.AccumulatedRegeneration += health.RegenerationRate * deltaTime;

        if (!(health.AccumulatedRegeneration >= 1.0f)) 
            return;
        
        int regenToApply = (int)health.AccumulatedRegeneration;
        int newValue = Math.Min(health.Max, health.Current + regenToApply);

        if (newValue == health.Current) 
            return;
        
        health.Current = newValue;
        health.AccumulatedRegeneration -= regenToApply;
        dirty.MarkDirty(DirtyComponentType.Health);
    }

    [Query]
    [All<Mana, DirtyFlags>]
    [None<Dead>]
    private void ProcessManaRegeneration(in Entity e, ref Mana mana, ref DirtyFlags dirty, [Data] float deltaTime)
    {
        if (mana.Current >= mana.Max)
        {
            mana.AccumulatedRegeneration = 0f;
            return;
        }

        mana.AccumulatedRegeneration += mana.RegenerationRate * deltaTime;

        if (mana.AccumulatedRegeneration >= 1.0f)
        {
            int regenToApply = (int)mana.AccumulatedRegeneration;
            int newValue = Math.Min(mana.Max, mana.Current + regenToApply);

            if (newValue != mana.Current)
            {
                mana.Current = newValue;
                mana.AccumulatedRegeneration -= regenToApply;
                dirty.MarkDirty(DirtyComponentType.Mana);
            }
        }
    }

    [Query]
    [All<Health, CombatState>]
    [None<Dead>]
    private void ProcessDeath(
        in Entity entity,
        ref Health health,
        ref CombatState combat,
        ref DirtyFlags dirty)
    {
        if (health.Current > 0)
            return;

        World.Add<Dead>(entity);
        combat.InCombat = false;
        dirty.MarkDirty(DirtyComponentType.Health | DirtyComponentType.CombatState);

        if (World.TryGet(entity, out NetworkId netId))
        {
            logger?.LogInformation("Entity {NetworkId} died", netId.Value);

            if (World.Has<PlayerControlled>(entity))
            {
                logger?.LogInformation(
                    "Player {NetworkId} will be revived in {Time}s",
                    netId.Value,
                    5f);
            }
        }
    }
}
