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
    
    /// <summary>
    /// Processa ataques de jogadores (PlayerControlled).
    /// O tipo de ataque é determinado pela vocação do jogador.
    /// </summary>
    [Query]
    [All<PlayerControlled, Input, PlayerInfo>]
    [None<Dead, Attack>]
    private void ProcessPlayerAttack(
        in Entity e,
        in Attackable atk,
        in PlayerInfo playerInfo,
        ref Input input,
        ref CombatState combat,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        ProcessEntityAttack(e, atk, playerInfo.VocationId, ref input, ref combat, ref dirty, deltaTime);
    }
    
    /// <summary>
    /// Processa ataques de NPCs (AIControlled).
    /// O tipo de ataque é determinado pela vocação do NPC.
    /// </summary>
    [Query]
    [All<AIControlled, Input, NpcInfo>]
    [None<Dead, Attack>]
    private void ProcessNpcAttack(
        in Entity e,
        in Attackable atk,
        in NpcInfo npcInfo,
        ref Input input,
        ref CombatState combat,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        ProcessEntityAttack(e, atk, npcInfo.VocationId, ref input, ref combat, ref dirty, deltaTime);
    }
    
    /// <summary>
    /// Lógica compartilhada para processar ataque de qualquer entidade.
    /// </summary>
    private void ProcessEntityAttack(
        in Entity e,
        in Attackable atk,
        byte vocationId,
        ref Input input,
        ref CombatState combat,
        ref DirtyFlags dirty,
        float deltaTime)
    {
        // Reduz cooldown (helper mantém clamped)
        CombatLogic.ReduceCooldown(ref combat, deltaTime);
        
        // Se a entidade não ativou a flag de ataque, sair
        if ((input.Flags & InputFlags.BasicAttack) == 0) return;
        
        // Se estiver em cooldown, sair silenciosamente
        if (!CombatLogic.CheckAttackCooldown(in combat))
            return;
        
        // Determina o tipo de ataque baseado na vocação
        AttackType attackType = CombatLogic.GetBasicAttackTypeForVocation(vocationId);
        
        combat.LastAttackTime = CombatLogic
            .CalculateAttackCooldownSeconds(
                atk, 
                attackType, 
                externalMultiplier: 1f);
        
        dirty.MarkDirty(DirtyComponentType.Combat);
        
        logger?.LogDebug("[CombatSystem] Entity attacking with {Attack} (Vocation: {Vocation}). Next cooldown: {Cooldown:F2}s", 
            attackType, vocationId, combat.LastAttackTime);
        
        World.Add<Attack>(e, new Attack
        {
            Type = attackType,
            RemainingDuration = CombatLogic.GetAttackTypeSpeedMultiplier(attackType),
            TotalDuration = CombatLogic.GetAttackTypeSpeedMultiplier(attackType),
            DamageApplied = false,
        });
    }
    
}