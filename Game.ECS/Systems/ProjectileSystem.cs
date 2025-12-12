using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Schema;
using Game.ECS.Services.Map;
using Microsoft.Extensions.Logging;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por mover projéteis e aplicar dano quando atingem alvos.
/// Projéteis se movem em direção ao alvo e são destruídos após colisão ou timeout.
/// </summary>
public sealed partial class ProjectileSystem(World world, 
    IMapIndex mapIndex, ILogger<ProjectileSystem>? logger = null)
    : GameSystem(world, logger)
{
    // Entities to destroy after query iteration
    private readonly List<Entity> _projectilesToDestroy = new(16);

    public override void Update(in float deltaTime)
    {
        MoveProjectilesQuery(World, deltaTime);
        
        // Destroy expired projectiles outside of query
        foreach (var projectile in _projectilesToDestroy)
        {
            if (World.IsAlive(projectile))
                World.Destroy(projectile);
        }
        _projectilesToDestroy.Clear();
    }

    /// <summary>
    /// Moves projectiles towards their target and checks for collisions.
    /// </summary>
    [Query]
    [All<Projectile, Position, Direction, MapId>]
    private void MoveProjectiles(
        in Entity entity,
        ref Projectile projectile,
        ref Position position,
        in Direction direction,
        in MapId mapId,
        [Data] float deltaTime)
    {
        // Already hit something
        if (projectile.HasHit)
        {
            _projectilesToDestroy.Add(entity);
            return;
        }
        
        // Update lifetime
        projectile.RemainingLifetime -= deltaTime;
        if (projectile.RemainingLifetime <= 0f)
        {
            _projectilesToDestroy.Add(entity);
            return;
        }
        
        // Calculate movement
        float moveAmount = projectile.Speed * deltaTime;
        
        // Direction to target
        float dx = projectile.TargetPosition.X - projectile.CurrentX;
        float dy = projectile.TargetPosition.Y - projectile.CurrentY;
        float distance = MathF.Sqrt(dx * dx + dy * dy);
        
        // Reached target
        if (distance <= moveAmount)
        {
            // Snap to target
            projectile.CurrentX = projectile.TargetPosition.X;
            projectile.CurrentY = projectile.TargetPosition.Y;
            position.X = projectile.TargetPosition.X;
            position.Y = projectile.TargetPosition.Y;
            
            // Try to hit target at position
            TryHitTargetAtPosition(entity, ref projectile, position, mapId.Value);
            _projectilesToDestroy.Add(entity);
            return;
        }
        
        // Normalize and move
        float nx = dx / distance;
        float ny = dy / distance;
        
        projectile.CurrentX += nx * moveAmount;
        projectile.CurrentY += ny * moveAmount;
        
        // Update grid position
        int newX = (int)MathF.Round(projectile.CurrentX);
        int newY = (int)MathF.Round(projectile.CurrentY);
        
        // Check if grid position changed
        if (newX != position.X || newY != position.Y)
        {
            position.X = newX;
            position.Y = newY;
            
            // Check for collision at new position
            if (CheckCollision(ref projectile, position, mapId.Value))
            {
                _projectilesToDestroy.Add(entity);
            }
        }
    }

    /// <summary>
    /// Checks if projectile hit something at the current position.
    /// </summary>
    private bool CheckCollision(ref Projectile projectile, Position position, int mapId)
    {
        if (!mapIndex.HasMap(mapId))
            return true; // Destroy if no map
        
        var grid = mapIndex.GetMapGrid(mapId);
        var spatial = mapIndex.GetMapSpatial(mapId);
        
        // Check map collision
        if (grid.IsBlocked(position))
        {
            projectile.HasHit = true;
            return true;
        }
        
        // Check entity collision
        if (spatial.TryGetFirstAt(position, out var hitEntity))
        {
            // Don't hit source
            if (hitEntity != projectile.Source && hitEntity != Entity.Null)
            {
                ApplyProjectileDamage(ref projectile, hitEntity);
                return true;
            }
        }
        
        return false;
    }

    /// <summary>
    /// Tries to hit a target at the final target position.
    /// </summary>
    private void TryHitTargetAtPosition(Entity projectileEntity, ref Projectile projectile, Position targetPos, int mapId)
    {
        if (!mapIndex.HasMap(mapId))
            return;
        
        var spatial = mapIndex.GetMapSpatial(mapId);
        
        if (spatial.TryGetFirstAt(targetPos, out var hitEntity))
            if (hitEntity != projectile.Source && hitEntity != Entity.Null)
                ApplyProjectileDamage(ref projectile, hitEntity);
    }

    /// <summary>
    /// Applies damage from projectile to target entity.
    /// </summary>
    private void ApplyProjectileDamage(ref Projectile projectile, Entity target)
    {
        if (!World.Has<Health>(target))
            return;
        
        if (World.Has<Invulnerable>(target))
            return;
        
        // Calculate damage with defense
        int damage = projectile.Damage;
        if (World.TryGet<CombatStats>(target, out var targetStats))
        {
            int defense = projectile.IsMagical ? targetStats.MagicDefense : targetStats.Defense;
            damage = Math.Max(1, damage - defense);
        }
        
        // Apply deferred damage
        DamageSystem.ApplyDeferredDamage(World, target, damage, false, projectile.Source);
        
        projectile.HasHit = true;
        
        logger?.LogDebug("[Projectile] Hit {Target} for {Damage} damage (magical: {IsMagical})", 
            target, damage, projectile.IsMagical);
    }
}
