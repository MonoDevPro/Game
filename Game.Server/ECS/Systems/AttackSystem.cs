using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class AttackSystem(World world, IMapService mapService, ILogger<AttackSystem>? logger = null) 
    : GameSystem(world)
{
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

        var attackType = AttackType.Basic;
        
        float cooldown = atk.CalculateAttackCooldownSeconds(attackType);
        combat.LastAttackTime = cooldown;
        combat.InCombat = true;
        dirty.MarkDirty(DirtyComponentType.Combat);
        
        World.Add<Attack>(e, new Attack
        {
            Type = attackType,
            RemainingDuration = SimulationConfig.DefaultAttackAnimationDuration,
            TotalDuration = SimulationConfig.DefaultAttackAnimationDuration,
            DamageApplied = false,
        });
    }
    
    [Query]
    [All<Attack>]
    [None<Dead>]
    private void ProcessAttackDamage(
        in Entity e,
        in MapId mapId,
        in Position position,
        in Facing facing,
        ref Attack atkAction,
        ref CombatState combat,
        [Data] float deltaTime)
    {
        // Reduz o tempo restante da animação
        atkAction.RemainingDuration -= deltaTime;
        
        // Se a animação terminou, remove o componente
        if (atkAction.RemainingDuration <= 0f)
        {
            World.Remove<Attack>(e);
            return;
        }
        
        // Verifica se chegou o momento de aplicar o dano
        if (!atkAction.ShouldApplyDamage())
            return;
        
        atkAction.DamageApplied = true;
        
        var targetPosition = position with { X = position.X + facing.DirectionX, Y = position.Y + facing.DirectionY };
        var spatial = mapService.GetMapSpatial(mapId.Value);
        if (spatial.TryGetFirstAt(targetPosition, out Entity foundEntity))
            if (World.TryAttack(e, foundEntity, atkAction.Type, out int damage))
            {
                World.ApplyDeferredDamage(e, foundEntity, damage, atkAction.Type == AttackType.Critical);
                
                combat.InCombat = true;
                combat.TimeSinceLastHit = 0f;
            }
    }
}