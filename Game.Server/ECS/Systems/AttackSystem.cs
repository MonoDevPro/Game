using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class AttackSystem(World world, IMapService mapService, ILogger<AttackSystem>? logger = null) 
    : GameSystem(world)
{
    private const float BaseAttackAnimationDuration = 1f;
    
    [Query]
    [All<PlayerControlled, PlayerInput>]
    [None<Dead>]
    private void ProcessPlayerAttack(in Entity e,
        ref PlayerInput input,
        ref CombatState combat,
        ref DirtyFlags dirty,
        in Attackable atk,
        in Facing facing,
        in Position position,
        in MapId mapId,
        [Data] float deltaTime)
    {
        // Reduz cooldown (helper mantém clamped)
        combat.ReduceCooldown(deltaTime);
        
        // Se o player não ativou a flag de ataque, sair
        if ((input.Flags & InputFlags.Attack) == 0) return;
        
        logger?.LogDebug("[AttackSystem] Player triggered attack. Cooldown: {Cooldown}", combat.LastAttackTime);
        
        // Se estiver em cooldown, sair
        if (!CombatLogic.CheckAttackCooldown(in combat))
        {
            logger?.LogDebug("[AttackSystem] Attack blocked by cooldown");
            return;
        }
        
        // Identifica o alvo à frente do jogador
        var targetPosition = position with
        {
            X = position.X + facing.DirectionX, 
            Y = position.Y + facing.DirectionY
        };
        logger?.LogDebug("[AttackSystem] Looking for target at ({X}, {Y})", targetPosition.X, targetPosition.Y);
        
        var spatial = mapService.GetMapSpatial(mapId.Value);
        if (!spatial.TryGetFirstAt(targetPosition, out Entity foundEntity))
            logger?.LogDebug("[AttackSystem] No target found at the position");
        
        combat.ApplyAttackState(in atk, AttackType.Basic);
        
        World.Add<Attack>(e, new Attack
        {
            TargetEntity = foundEntity,
            Type = AttackType.Basic,
            RemainingDuration = BaseAttackAnimationDuration,
            TotalDuration = BaseAttackAnimationDuration,
            DamageApplied = false,
        });
        dirty.MarkDirty(DirtyComponentType.CombatState);
    }
    
    [Query]
    [All<PlayerControlled, Attack>]
    [None<Dead>]
    private void ProcessAttackDamage(
        in Entity attacker,
        ref Attack atkAction,
        [Data] float deltaTime)
    {
        // Reduz o tempo restante da animação
        atkAction.RemainingDuration -= deltaTime;
        
        // Se a animação terminou, remove o componente
        if (atkAction.RemainingDuration <= 0f)
        {
            World.Remove<Attack>(attacker);
            return;
        }
        
        // Verifica se chegou o momento de aplicar o dano
        if (!atkAction.ShouldApplyDamage())
            return;
        
        // Aplica o dano se encontrou o alvo
        if (World.TryAttack(attacker, atkAction.TargetEntity, atkAction.Type, out int damage))
            World.ApplyDeferredDamage(attacker, atkAction.TargetEntity, damage, atkAction.Type == AttackType.Critical);
        
        atkAction.DamageApplied = true;
    }
}