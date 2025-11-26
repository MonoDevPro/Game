using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Systems;
using System;

namespace Game.Server.ECS.Systems;

public sealed partial class CombatResolutionSystem(World world) : GameSystem(world)
{
    [Query]
    [All<AttackCommand, CombatStats, Position>]
    public void ResolveAttacks(Entity attacker, ref AttackCommand cmd, in CombatStats stats, in Position pos)
    {
        if (!cmd.IsReady) return;

        // 1. Validação Final (O alvo ainda está lá? Está vivo?)
        if (!World.IsAlive(cmd.Target) || !World.TryGet(cmd.Target, out Health targetHealth))
        {
            World.Remove<AttackCommand>(attacker);
            return;
        }

        // 2. Cálculo de Dano (Data-Driven)
        // Assuming Physical for now. Ideally AttackCommand should carry the type.
        int damageRoll = stats.AttackPower;
        // Add some variance +/- 10%
        damageRoll = (int)(damageRoll * (0.9f + Random.Shared.NextSingle() * 0.2f));
        
        if (World.TryGet(cmd.Target, out CombatStats targetStats))
        {
            damageRoll = Math.Max(0, damageRoll - targetStats.Defense);
        }

        // 3. Aplicação
        targetHealth.Current -= damageRoll;
        World.Set(cmd.Target, targetHealth); // Update component
        
        // 4. Feedback (Logs, Eventos de Rede para Clientes)
        // TODO: Send network packet
        
        // 5. Limpeza
        World.Remove<AttackCommand>(attacker); 
        
        // Se matou, notifica sistemas (XP, Loot, etc) via mensagens ou checks posteriores
        if (targetHealth.Current <= 0)
        {
            // HandleDeath(cmd.Target);
            // Usually handled by DeathSystem checking Health <= 0
        }
    }
}
