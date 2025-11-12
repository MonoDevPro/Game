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

        // ✅ Marca como morto
        World.Add<Dead>(entity);
        
        // ✅ Reseta combate
        combat.InCombat = false;
        
        dirty.MarkDirty(DirtyComponentType.Health | DirtyComponentType.CombatState);

        if (World.TryGet(entity, out NetworkId netId))
        {
            logger?.LogInformation("Entity {NetworkId} died", netId.Value);
        
            // ✅ Se for jogador, o ReviveSystem cuidará do resto
            if (World.Has<PlayerControlled>(entity))
            {
                logger?.LogInformation(
                    "Player {NetworkId} will be revived in {Time}s",
                    netId.Value,
                    5f); // DefaultReviveTime
            }
        }
    }
}
