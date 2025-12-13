using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.DTOs.Game.Player;
using Game.ECS.Archetypes;
using Game.ECS.Components;
using Game.ECS.Events;
using Game.ECS.Helpers;
using Game.ECS.Services.Map;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por processar comandos de ataque e aplicar dano.
/// Suporta ataques melee, ranged e mágicos baseados na vocação/estilo.
/// </summary>
public sealed partial class CombatSystem(World world, MapIndex mapIndex, ILogger<CombatSystem>? logger = null)
    : GameSystem(world, logger)
{
    // Projectile settings
    private const float ProjectileSpeed = 15f;
    private const float ProjectileLifetime = 3f;

    /// <summary>
    /// Processes attack input for player-controlled entities.
    /// When BasicAttack flag is set, creates an attack command.
    /// </summary>
    [Query]
    [All<PlayerControlled, Input, Position, Direction, CombatStats, CombatState, VocationId, MapId>]
    [None<Dead, AttackCommand>]
    private void ProcessPlayerAttackInput(
        in Entity entity,
        in Input input,
        in Position position,
        in Direction direction,
        in CombatStats stats,
        ref CombatState state,
        in VocationId vocation,
        [Data] float deltaTime)
    {
        // Update cooldown timer
        if (state.InCooldown)
        {
            state.AttackCooldownTimer -= deltaTime;
            if (state.AttackCooldownTimer <= 0f)
            {
                state.InCooldown = false;
                state.AttackCooldownTimer = 0f;
            }
        }
        
        // Check if attack input is pressed
        if ((input.Flags & InputFlags.BasicAttack) == 0)
            return;
        
        // Check cooldown
        if (state.InCooldown)
            return;
        
        // Determine attack style based on vocation
        var attackStyle = AttackHelpers.GetAttackStyleFromVocation(vocation.Value);
        
        // Calculate target position based on direction and range
        var range = stats.AttackRange;
        var targetPosition = new Position
        {
            X = position.X + (int)(direction.X * range),
            Y = position.Y + (int)(direction.Y * range)
        };
        
        // Set cooldown
        state.InCooldown = true;
        state.AttackCooldownTimer = 1f / stats.AttackSpeed;
        
        // Create attack command
        World.Add(entity, new AttackCommand
        {
            Target = Entity.Null,
            TargetPosition = targetPosition,
            Style = attackStyle,
            ConjureDuration = 1f, // TODO: Placeholder, can be adjusted per style
        });
        
        var attackEvent = new AttackEvent(entity, Entity.Null, attackStyle, stats.AttackPower);
        EventBus.Send(ref attackEvent);
    }

    /// <summary>
    /// Processes attack commands and applies damage or creates projectiles.
    /// </summary>
    [Query]
    [All<AttackCommand, Position, Direction, CombatStats, MapId>]
    [None<Dead>]
    private void ProcessAttackCommand(
        [Data] in float deltaTime,
        in Entity attacker,
        ref AttackCommand command,
        in Position attackerPos,
        in Direction attackerDir,
        in CombatStats attackerStats,
        in MapId attackerMapId)
    {
        if (command.ConjureDuration > 0f)
        {
            command.ConjureDuration -= deltaTime;
            return;
        }
        
        switch (command.Style)
        {
            case AttackStyle.Melee:
                ProcessMeleeAttack(attacker, command, attackerPos, attackerMapId, attackerStats);
                break;
            case AttackStyle.Ranged:
            case AttackStyle.Magic:
                CreateProjectile(attacker, command, attackerPos, attackerDir, attackerMapId, attackerStats);
                break;
        }
        
        // Remove command after processing
        World.Remove<AttackCommand>(attacker);
    }

    /// <summary>
    /// Processes melee attack - applies damage immediately if target in range.
    /// </summary>
    private void ProcessMeleeAttack(
        Entity attacker,
        in AttackCommand command,
        in Position attackerPos,
        in MapId attackerMapId,
        in CombatStats attackerStats)
    {
        // Check if we have a valid target
        if (command.Target == Entity.Null || !World.IsAlive(command.Target))
        {
            // Try to find target at position
            if (!mapIndex.HasMap(attackerMapId.Value))
                return;
            
            var spatial = mapIndex.GetMapSpatial(attackerMapId.Value);
            if (!spatial.TryGetFirstAt(command.TargetPosition, out var target) || target == attacker)
                return;
            
            ApplyDamageToTarget(attacker, target, attackerStats, false);
        }
        else
        {
            // Verify target is still in range
            if (!World.TryGet<Position>(command.Target, out var targetPos))
                return;
            
            var distance = Math.Abs(attackerPos.X - targetPos.X) + Math.Abs(attackerPos.Y - targetPos.Y);
            if (distance > attackerStats.AttackRange)
                return;
            
            ApplyDamageToTarget(attacker, command.Target, attackerStats, false);
        }
    }

    /// <summary>
    /// Creates a projectile entity for ranged/magic attacks.
    /// </summary>
    private void CreateProjectile(
        Entity source,
        in AttackCommand command,
        in Position sourcePos,
        in Direction sourceDir,
        in MapId sourceMapId,
        in CombatStats sourceStats)
    {
        bool isMagical = command.Style == AttackStyle.Magic;
        int damage = isMagical ? sourceStats.MagicPower : sourceStats.AttackPower;
        
        var projectile = World.Create(ProjectileArchetypes.Projectile);
        
        World.Set(projectile, new Position { X = sourcePos.X, Y = sourcePos.Y, Z = sourcePos.Z });
        World.Set(projectile, new Direction { X = sourceDir.X, Y = sourceDir.Y });
        World.Set(projectile, new Speed { Value = ProjectileSpeed });
        World.Set(projectile, new MapId { Value = sourceMapId.Value });
        World.Set(projectile, new Projectile
        {
            Source = source,
            TargetPosition = command.TargetPosition,
            CurrentX = sourcePos.X,
            CurrentY = sourcePos.Y,
            Speed = ProjectileSpeed,
            Damage = damage,
            IsMagical = isMagical,
            RemainingLifetime = ProjectileLifetime,
            HasHit = false
        });
        
        logger?.LogDebug("[Combat] Created projectile from {Source} towards ({X}, {Y})", 
            source, command.TargetPosition.X, command.TargetPosition.Y);
    }

    /// <summary>
    /// Applies damage to a target entity using deferred damage.
    /// </summary>
    private void ApplyDamageToTarget(Entity attacker, Entity target, in CombatStats attackerStats, bool isMagical)
    {
        if (!World.Has<Health>(target))
            return;
        
        // Check if target is invulnerable
        if (World.Has<Invulnerable>(target))
            return;
        
        // Calculate damage
        int baseDamage = isMagical ? attackerStats.MagicPower : attackerStats.AttackPower;
        int defense = 0;
        
        if (World.TryGet<CombatStats>(target, out var targetStats))
        {
            defense = isMagical ? targetStats.MagicDefense : targetStats.Defense;
        }
        
        int finalDamage = Math.Max(1, baseDamage - defense);
        
        // Apply deferred damage
        DamageSystem.ApplyDeferredDamage(World, target, finalDamage, false, attacker);
        
        logger?.LogDebug("[Combat] {Attacker} dealt {Damage} damage to {Target}", attacker, finalDamage, target);
    }
}
