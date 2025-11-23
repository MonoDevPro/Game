using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema que aplica dano periódico (DoT) em entidades com Health + DamageOverTime.
/// Usa VitalsLogic.ApplyPeriodicDamage para acumular frações de dano.
/// </summary>
public sealed partial class DamageSystem(World world, IMapService mapService) : GameSystem(world)
{
    [Query]
    [All<Attack>]
    [None<Dead>]
    private void ProcessAttackDamage(
        in Entity attacker,
        in MapId mapId,
        in Position position,
        in Floor floor,
        in Facing facing,
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
        if (!CombatLogic.ShouldApplyDamage(atkAction))
            return;
        
        atkAction.DamageApplied = true;
        
        SpatialPosition targetSpatialPosition = new(
            position.X + facing.DirectionX, 
            position.Y + facing.DirectionY, 
            floor.Level);
        
        var spatial = mapService.GetMapSpatial(mapId.Value);
        if (spatial.TryGetFirstAt(targetSpatialPosition, out Entity foundEntity))
        {
            if (!CombatLogic.CheckAttackDistance(in position, new Position(targetSpatialPosition.X, targetSpatialPosition.Y), atkAction.Type))
                return;
            
            var damage = CombatLogic.CalculateDamage(World, in attacker, in foundEntity, atkAction.Type, isCritical: false);
            
            DamageLogic.ApplyDeferredDamage(World, in foundEntity, damage, isCritical: false, attacker: attacker);
            
            CombatLogic.EnterCombat(World, in attacker);
            CombatLogic.EnterCombat(World, in foundEntity);
        }
    }
    
    [Query]
    [All<Health, DamageOverTime, DirtyFlags>]
    [None<Dead, Invulnerable>]
    private void ProcessDamageOverTime(
        in Entity entity,
        ref Health health,
        ref DamageOverTime dot,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        // Atualiza tempo restante do efeito
        dot.RemainingTime -= deltaTime;

        // Aplica dano periódico (acumulado)
        bool changed = DamageLogic.ApplyPeriodicDamage(
            ref health.Current,
            dot.DamagePerSecond,
            deltaTime,
            ref dot.AccumulatedDamage);

        if (changed)
            dirty.MarkDirty(DirtyComponentType.Vitals);

        // Se HP chegou a zero ou o tempo do efeito acabou, remove o DoT
        if (health.Current <= 0 || dot.RemainingTime <= 0f)
            World.Remove<DamageOverTime>(entity);
    }
    
    [Query]
    [All<Damaged, Health>]
    [None<Dead, Invulnerable>]
    private void ProcessDeferredDamage(
        in Entity victim,
        in Damaged damaged,
        ref Health health,
        ref DirtyFlags dirty)
    {
        // ✅ Aplica o dano
        if (DamageLogic.TryDamage(ref health, damaged.Amount))
            dirty.MarkDirty(DirtyComponentType.Vitals);
        
        World.Remove<Damaged>(victim);
    }
}