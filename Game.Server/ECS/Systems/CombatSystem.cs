using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por atualizar o estado de combate dos jogadores.
/// Mantém um timer desde o último hit e limpa o flag de combate
/// após um período configurado, permitindo voltar a regenerar HP/MP.
/// </summary>
public sealed partial class CombatSystem(World world, ILogger<CombatSystem>? logger = null) : GameSystem(world)
{
    [Query]
    [All<CombatState>]
    [None<Dead>] // morto não entra/sai de combate
    private void UpdateCombatState(ref CombatState combat, [Data] float deltaTime)
    {
        if (!combat.InCombat)
            return;

        combat.TimeSinceLastHit += deltaTime;

        if (combat.TimeSinceLastHit >= SimulationConfig.HealthRegenDelayAfterCombat)
        {
            combat.InCombat = false;
            combat.TimeSinceLastHit = SimulationConfig.HealthRegenDelayAfterCombat;
        }
    }
    
    [Query]
    [All<Input>]
    [None<Dead, Attack>]
    private void ProcessAttack(
        in Entity e,
        in Attackable atk,
        ref Input input,
        ref CombatState combat,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        // Reduz cooldown (helper mantém clamped)
        CombatLogic.ReduceCooldown(ref combat, deltaTime);
        
        // Se o player não ativou a flag de ataque, sair
        if ((input.Flags & InputFlags.BasicAttack) == 0) return;
        
        const AttackType attackType = AttackType.Basic;
        
        logger?.LogDebug("[AttackSystem] Entity triggered {Attack}. Cooldown: {Cooldown}", attackType, combat.LastAttackTime);
        
        // Se estiver em cooldown, sair
        if (!CombatLogic.CheckAttackCooldown(in combat))
        {
            logger?.LogDebug("[AttackSystem] Attack blocked by cooldown");
            return;
        }
        
        combat.LastAttackTime = CombatLogic
            .CalculateAttackCooldownSeconds(
                atk, 
                attackType, 
                externalMultiplier: 1f);
        
        dirty.MarkDirty(DirtyComponentType.Combat);
        
        World.Add<Attack>(e, new Attack
        {
            Type = attackType,
            RemainingDuration = CombatLogic.GetAttackTypeSpeedMultiplier(attackType),
            TotalDuration = CombatLogic.GetAttackTypeSpeedMultiplier(attackType),
            DamageApplied = false,
        });
    }
    
}