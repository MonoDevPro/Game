using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Systems.Common;

namespace Game.ECS.Systems;

public sealed partial class HealthRegenerationSystem(World world) : GameSystem(world)
{
    [Query]
    [All<Health, CombatState>]
    [None<Dead>]
    private void RegenerateHealth(in Entity e, ref Health health, in CombatState combatState, [Data] float deltaTime)
    {
        // Apenas regenera fora de combate
        if (!combatState.InCombat && health.Current < health.Max)
        {
            health.Current = Math.Min(
                health.Max, 
                health.Current + (int)(health.RegenerationRate * deltaTime)
            );
            World.MarkNetworkDirty(e, SyncFlags.Vitals);
        }
    }
}