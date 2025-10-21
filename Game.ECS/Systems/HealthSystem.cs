using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Utils;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por regeneração de vida e mana.
/// Processa entidades que têm Health e Mana, aplicando regeneração por tick.
/// </summary>
public sealed partial class HealthSystem(World world) : GameSystem(world)
{
    [Query]
    [All<Health>]
    private void ProcessHealthRegeneration(in Entity e, ref Health health, [Data] float deltaTime)
    {
        if (health.Current >= health.Max)
            return;

        float regeneration = health.RegenerationRate * deltaTime;
        health.Current = Math.Min(health.Current + (int)regeneration, health.Max);
        
        // Marca como dirty para sincronização
        World.MarkNetworkDirty(e, SyncFlags.Vitals);
    }

    [Query]
    [All<Mana>]
    private void ProcessManaRegeneration(in Entity e, ref Mana mana, [Data] float deltaTime)
    {
        if (mana.Current >= mana.Max)
            return;

        float regeneration = mana.RegenerationRate * deltaTime;
        mana.Current = Math.Min(mana.Current + (int)regeneration, mana.Max);
        
        // Marca como dirty para sincronização
        World.MarkNetworkDirty(e, SyncFlags.Vitals);
    }

    [Query]
    [All<Dead>]
    private void ProcessDeadEntities(in Entity e, ref CombatState combat, [Data] float deltaTime)
    {
        // Aqui pode haver lógica adicional para entidades mortas
        // Por exemplo: ragdoll timeout, respawn timer, etc
    }
}
