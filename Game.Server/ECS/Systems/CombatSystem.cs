using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável pela lógica de combate: ataque, dano, morte.
/// Processa ataques, aplica dano e gerencia transições para estado Dead.
/// </summary>
public sealed partial class CombatSystem(World world) 
    : GameSystem(world)
{
    [Query]
    [All<Health, CombatState>]
    [None<Dead>]
    private void ProcessTakeDamage(in Entity e, ref Health health, ref CombatState combat, [Data] float deltaTime)
    {
        if (health.Current <= 0)
        {
            health.Current = 0;
            World.Add<Dead>(e);
        }
    }

    [Query]
    [All<Attackable, CombatState, AttackPower>]
    [None<Dead>]
    private void ProcessAttackCooldown(in Entity e, ref CombatState combat, [Data] float deltaTime)
    {
        if (combat.LastAttackTime > 0)
            combat.LastAttackTime -= deltaTime;
    }
}