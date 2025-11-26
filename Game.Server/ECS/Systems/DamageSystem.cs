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
/// Também processa ataques melee (imediato) e ranged (cria projétil).
/// </summary>
public sealed partial class DamageSystem(World world, IMapService mapService, ILogger<DamageSystem>? logger = null) : GameSystem(world)
{
    /*
    /// <summary>
    /// Processa ataques melee de jogadores (dano imediato na célula adjacente).
    /// </summary>
    [Query]
    [All<PlayerControlled, Attack, PlayerInfo>]
    [None<Dead>]
    private void ProcessPlayerMeleeAttackDamage(
        in Entity attacker,
        in MapId mapId,
        in Position position,
        in Floor floor,
        in Facing facing,
        in PlayerInfo playerInfo,
        ref Attack atkAction,
        [Data] float deltaTime)
    {
        // Apenas Warriors usam ataque melee
        if (CombatLogic.IsRangedVocation(playerInfo.VocationId))
        {
            ProcessRangedAttack(attacker, mapId, position, floor, facing, playerInfo.VocationId, ref atkAction, deltaTime);
        }
        else
        {
            ProcessMeleeAttack(attacker, mapId, position, floor, facing, ref atkAction, deltaTime);
        }
    }
    
    /// <summary>
    /// Processa ataques melee de NPCs (dano imediato na célula adjacente).
    /// </summary>
    [Query]
    [All<AIControlled, Attack, NpcInfo>]
    [None<Dead>]
    private void ProcessNpcMeleeAttackDamage(
        in Entity attacker,
        in MapId mapId,
        in Position position,
        in Floor floor,
        in Facing facing,
        in NpcInfo npcInfo,
        ref Attack atkAction,
        [Data] float deltaTime)
    {
        // NPCs ranged (Archer, Mage) disparam projéteis
        if (CombatLogic.IsRangedVocation(npcInfo.VocationId))
        {
            ProcessRangedAttack(attacker, mapId, position, floor, facing, npcInfo.VocationId, ref atkAction, deltaTime);
        }
        else
        {
            ProcessMeleeAttack(attacker, mapId, position, floor, facing, ref atkAction, deltaTime);
        }
    }
    
    /// <summary>
    /// Processa ataque melee: aplica dano na célula adjacente.
    /// </summary>
    private void ProcessMeleeAttack(
        in Entity attacker,
        in MapId mapId,
        in Position position,
        in Floor floor,
        in Facing facing,
        ref Attack atkAction,
        float deltaTime)
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
        
        // Melee: verifica célula adjacente na direção do facing
        SpatialPosition targetSpatialPosition = new(
            position.X + facing.DirectionX, 
            position.Y + facing.DirectionY, 
            floor.Level);
        
        var spatial = mapService.GetMapSpatial(mapId.Value);
        if (spatial.TryGetFirstAt(targetSpatialPosition, out Entity foundEntity))
        {
            var damage = CombatLogic.CalculateDamage(World, in attacker, in foundEntity, atkAction.Type, isCritical: false);
            
            DamageLogic.ApplyDeferredDamage(World, in foundEntity, damage, isCritical: false, attacker: attacker);
            
            CombatLogic.EnterCombat(World, in attacker);
            CombatLogic.EnterCombat(World, in foundEntity);
            
            logger?.LogDebug("[DamageSystem] Melee attack hit! Damage: {Damage}", damage);
        }
    }
    
    /// <summary>
    /// Processa ataque ranged: cria projétil em direção ao alvo.
    /// </summary>
    private void ProcessRangedAttack(
        in Entity attacker,
        in MapId mapId,
        in Position position,
        in Floor floor,
        in Facing facing,
        byte vocationId,
        ref Attack atkAction,
        float deltaTime)
    {
        // Reduz o tempo restante da animação
        atkAction.RemainingDuration -= deltaTime;
        
        // Se a animação terminou, remove o componente
        if (atkAction.RemainingDuration <= 0f)
        {
            World.Remove<Attack>(attacker);
            return;
        }
        
        // Verifica se chegou o momento de disparar o projétil
        if (!CombatLogic.ShouldApplyDamage(atkAction))
            return;
            
        atkAction.DamageApplied = true;
        
        // Cria o projétil
        // TODO: Calcular alvo baseado no facing ou target lock
        // Por enquanto, dispara reto
        Position targetPos = new(position.X + facing.DirectionX * 10, position.Y + facing.DirectionY * 10);
        
        bool isMagic = vocationId == (byte)VocationType.Mage;
        int damage = CombatLogic.CalculateDamage(World, in attacker, in Entity.Null, atkAction.Type, isCritical: false);
        
        // Cria entidade do projétil
        var projectile = World.Create(
            new Projectile
            {
                Source = attacker,
                TargetPosition = targetPos,
                CurrentX = position.X,
                CurrentY = position.Y,
                Speed = 10f, // Tiles/s
                Damage = damage,
                IsMagical = isMagic,
                RemainingLifetime = 2f,
                HasHit = false
            },
            new Position { X = position.X, Y = position.Y },
            new Floor { Level = floor.Level },
            new MapId { Value = mapId.Value }
        );
        
        logger?.LogDebug("[DamageSystem] Ranged attack fired! Damage: {Damage}", damage);
    }
    */
    
    /*
    /// <summary>
    /// Processa ataques melee de NPCs (dano imediato na célula adjacente).
    /// </summary>
    [Query]
    [All<AIControlled, Attack, NpcInfo>]
    [None<Dead>]
    private void ProcessNpcMeleeAttackDamage(
        in Entity attacker,
        in MapId mapId,
        in Position position,
        in Floor floor,
        in Facing facing,
        in NpcInfo npcInfo,
        ref Attack atkAction,
        [Data] float deltaTime)
    {
        // NPCs ranged (Archer, Mage) disparam projéteis
        if (CombatLogic.IsRangedVocation(npcInfo.VocationId))
        {
            ProcessRangedAttack(attacker, mapId, position, floor, facing, npcInfo.VocationId, ref atkAction, deltaTime);
        }
        else
        {
            ProcessMeleeAttack(attacker, mapId, position, floor, facing, ref atkAction, deltaTime);
        }
    }
    
    /// <summary>
    /// Processa ataque melee: aplica dano na célula adjacente.
    /// </summary>
    private void ProcessMeleeAttack(
        in Entity attacker,
        in MapId mapId,
        in Position position,
        in Floor floor,
        in Facing facing,
        ref Attack atkAction,
        float deltaTime)
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
        
        // Melee: verifica célula adjacente na direção do facing
        SpatialPosition targetSpatialPosition = new(
            position.X + facing.DirectionX, 
            position.Y + facing.DirectionY, 
            floor.Level);
        
        var spatial = mapService.GetMapSpatial(mapId.Value);
        if (spatial.TryGetFirstAt(targetSpatialPosition, out Entity foundEntity))
        {
            var damage = CombatLogic.CalculateDamage(World, in attacker, in foundEntity, atkAction.Type, isCritical: false);
            
            DamageLogic.ApplyDeferredDamage(World, in foundEntity, damage, isCritical: false, attacker: attacker);
            
            CombatLogic.EnterCombat(World, in attacker);
            CombatLogic.EnterCombat(World, in foundEntity);
            
            logger?.LogDebug("[DamageSystem] Melee attack hit! Damage: {Damage}", damage);
        }
    }
    
    /// <summary>
    /// Processa ataque ranged: cria projétil em direção ao alvo.
    /// </summary>
    private void ProcessRangedAttack(
        in Entity attacker,
        in MapId mapId,
        in Position position,
        in Floor floor,
        in Facing facing,
        byte vocationId,
        ref Attack atkAction,
        float deltaTime)
    {
        // Reduz o tempo restante da animação
        atkAction.RemainingDuration -= deltaTime;
        
        // Se a animação terminou, remove o componente
        if (atkAction.RemainingDuration <= 0f)
        {
            World.Remove<Attack>(attacker);
            return;
        }
        
        // Verifica se chegou o momento de disparar o projétil
        if (!CombatLogic.ShouldApplyDamage(atkAction))
            return;
        
        atkAction.DamageApplied = true;
        
        // Calcula posição alvo baseada no range da vocação
        int range = CombatLogic.GetAttackRangeForVocation(vocationId);
        Position targetPosition = new(
            position.X + (facing.DirectionX * range),
            position.Y + (facing.DirectionY * range));
        
        // Determina se é dano mágico
        bool isMagical = CombatLogic.IsMagicalAttackForVocation(vocationId);
        
        // Calcula dano base do projétil (sem considerar defesa ainda)
        int damage = CombatLogic.CalculateProjectileDamage(World, in attacker, isMagical, isCritical: false);
        
        // Cria o projétil
        var projectile = World.Create<Projectile, MapId, Floor>();
        World.Set(projectile, new Projectile
        {
            Source = attacker,
            TargetPosition = targetPosition,
            CurrentX = position.X,
            CurrentY = position.Y,
            Speed = CombatLogic.GetProjectileSpeed(),
            Damage = damage,
            IsMagical = isMagical,
            RemainingLifetime = CombatLogic.GetProjectileLifetime(),
            HasHit = false
        });
        World.Set(projectile, new MapId { Value = mapId.Value });
        World.Set(projectile, new Floor { Level = floor.Level });
        
        // Atacante entra em combate ao disparar
        CombatLogic.EnterCombat(World, in attacker);
        
        logger?.LogDebug("[DamageSystem] Ranged attack! Projectile created. Damage: {Damage}, Magical: {IsMagical}", damage, isMagical);
    }
    */
    
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