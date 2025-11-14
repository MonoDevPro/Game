using Arch.Core;
using Arch.LowLevel;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities.Repositories;
using Game.ECS.Extensions;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

public sealed partial class AttackSystem(World world, IMapService mapService, ILogger<AttackSystem>? logger = null) 
    : GameSystem(world)
{
    private const float BaseAttackAnimationDuration = 1f;
    
    private UnsafeStack<Entity> _targetBuffer = new(16);

    [Query]
    [All<PlayerControlled, PlayerInput>]
    [None<Attack, Dead>]
    private void ProcessPlayerAttack(
        in Entity e,
        ref PlayerInput input,
        ref CombatState combat,
        ref DirtyFlags dirty,
        in Attackable atk,
        [Data] float deltaTime)
    {
        // Reduz cooldown (helper mantém clamped)
        combat.ReduceCooldown(deltaTime);
        
        // Se o player não ativou a flag de ataque, sair
        if ((input.Flags & InputFlags.Attack) == 0) return;
        
        // Se estiver em cooldown, sair
        if (!CombatLogic.CheckAttackCooldown(in combat)) return;
        
        World.Add<Attack>(e, new Attack
        {
            Type = AttackType.Basic,
            RemainingDuration = BaseAttackAnimationDuration,
            TotalDuration = BaseAttackAnimationDuration,
            DamageApplied = false,
        });
        
        combat.ApplyAttackState(in atk, AttackType.Basic);
        
        dirty.MarkDirty(DirtyComponentType.CombatState);
    }
    
    [Query]
    [All<Attack>]
    [None<Dead, Invulnerable>]
    private void ProcessAttackDamage(
        in Entity attacker,
        ref Attack action,
        in Facing facing,
        in Position position,
        in MapId mapId,
        [Data] float deltaTime)
    {
        // Reduz o tempo restante da animação
        action.RemainingDuration -= deltaTime;
        
        // Verifica se chegou o momento de aplicar o dano
        if (!action.ShouldApplyDamage())
            return;
        
        var range = action.Type switch
        {
            AttackType.Basic    => 1,
            AttackType.Heavy    => 1,
            AttackType.Critical => 1,
            AttackType.Magic    => 3,
            _ => 1
        };
        var targetPosition = position with
        {
            X = position.X + facing.DirectionX * range,
            Y = position.Y + facing.DirectionY * range
        };
        
        var spatial = mapService.GetMapSpatial(mapId.Value);
        spatial.QueryAt(targetPosition, ref _targetBuffer);
        
        while (_targetBuffer.Count > 0)
        {
            var victim = _targetBuffer.Pop();
            if (World.TryAttack(attacker, victim, action.Type, out int damage))
                World.ApplyDeferredDamage(attacker, victim, damage, action.Type == AttackType.Critical);
        }
        action.DamageApplied = true;
        
        // Se a animação terminou, remove o componente
        if (action.RemainingDuration <= 0f)
            World.Remove<Attack>(attacker);
    }
}