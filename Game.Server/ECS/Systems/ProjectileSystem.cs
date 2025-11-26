using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Logic;
using Game.ECS.Services;
using Game.ECS.Systems;

namespace Game.Server.ECS.Systems;

/// <summary>
/// Sistema responsável por movimentar projéteis, detectar colisões e aplicar dano.
/// Projéteis são criados pelo DamageSystem para ataques ranged (Archer, Mage).
/// </summary>
public sealed partial class ProjectileSystem(World world, IMapService mapService, ILogger<ProjectileSystem>? logger = null) : GameSystem(world)
{
    private readonly List<Entity> _projectilesToDestroy = [];
    
    /// <summary>
    /// Processa o movimento e colisão de todos os projéteis ativos.
    /// </summary>
    [Query]
    [All<Projectile, MapId, Floor>]
    private void ProcessProjectile(
        in Entity projectileEntity,
        ref Projectile projectile,
        in MapId mapId,
        in Floor floor,
        [Data] float deltaTime)
    {
        // Se já atingiu algo, não processa mais
        if (projectile.HasHit)
        {
            _projectilesToDestroy.Add(projectileEntity);
            return;
        }
        
        // Reduz tempo de vida
        projectile.RemainingLifetime -= deltaTime;
        if (projectile.RemainingLifetime <= 0f)
        {
            logger?.LogDebug("[ProjectileSystem] Projectile expired (timeout)");
            _projectilesToDestroy.Add(projectileEntity);
            return;
        }
        
        // Calcula direção para o alvo
        float dx = projectile.TargetPosition.X - projectile.CurrentX;
        float dy = projectile.TargetPosition.Y - projectile.CurrentY;
        float distanceToTarget = MathF.Sqrt(dx * dx + dy * dy);
        
        // Se chegou muito perto do alvo, para
        if (distanceToTarget < 0.1f)
        {
            logger?.LogDebug("[ProjectileSystem] Projectile reached target position without hitting anything");
            _projectilesToDestroy.Add(projectileEntity);
            return;
        }
        
        // Normaliza direção e aplica velocidade
        float moveDistance = projectile.Speed * deltaTime;
        
        // Limita movimento para não ultrapassar o alvo
        if (moveDistance > distanceToTarget)
            moveDistance = distanceToTarget;
        
        float normalizedDx = dx / distanceToTarget;
        float normalizedDy = dy / distanceToTarget;
        
        // Atualiza posição
        projectile.CurrentX += normalizedDx * moveDistance;
        projectile.CurrentY += normalizedDy * moveDistance;
        
        // Converte para posição de grid para verificar colisão
        int gridX = (int)MathF.Round(projectile.CurrentX);
        int gridY = (int)MathF.Round(projectile.CurrentY);
        
        // Verifica colisão com entidades no tile atual
        var spatial = mapService.GetMapSpatial(mapId.Value);
        SpatialPosition currentSpatialPos = new(gridX, gridY, floor.Level);
        
        if (spatial.TryGetFirstAt(currentSpatialPos, out Entity foundEntity))
        {
            // Não acerta a si mesmo
            if (foundEntity == projectile.Source)
                return;
            
            // Verifica se a entidade pode ser atacada (tem Health)
            if (!World.Has<Health>(foundEntity))
                return;
            
            // Verifica se a entidade não está morta
            if (World.Has<Dead>(foundEntity))
                return;
                
            // Verifica se a entidade não é invulnerável
            if (World.Has<Invulnerable>(foundEntity))
                return;
            
            // Projétil atingiu o alvo!
            projectile.HasHit = true;
            
            // Aplica dano (considerando defesa do alvo)
            int finalDamage = ApplyProjectileDamage(foundEntity, projectile.Damage, projectile.IsMagical);
            
            logger?.LogDebug("[ProjectileSystem] Projectile hit! Final damage: {Damage} (magical: {IsMagical})", 
                finalDamage, projectile.IsMagical);
            
            _projectilesToDestroy.Add(projectileEntity);
        }
    }
    
    /// <summary>
    /// Aplica dano do projétil considerando a defesa do alvo.
    /// </summary>
    private int ApplyProjectileDamage(Entity target, int baseDamage, bool isMagical)
    {
        int finalDamage = baseDamage;
        
        // Aplica redução de defesa
        if (World.TryGet<CombatStats>(target, out var stats))
        {
            int defensePower = isMagical ? stats.MagicDefense : stats.Defense;
            finalDamage = Math.Max(1, baseDamage - defensePower);
        }
        
        // Aplica variância de dano
        float variance = 0.9f + (float)Random.Shared.NextDouble() * 0.2f;
        finalDamage = (int)(finalDamage * variance);
        finalDamage = Math.Max(1, finalDamage);
        
        // Aplica dano de forma diferida
        DamageLogic.ApplyDeferredDamage(World, in target, finalDamage, isCritical: false, attacker: Entity.Null);
        
        return finalDamage;
    }
    
    /// <summary>
    /// Remove projéteis marcados para destruição após a query.
    /// </summary>
    public override void AfterUpdate(in float deltaTime)
    {
        base.AfterUpdate(deltaTime);
        
        foreach (var projectile in _projectilesToDestroy)
        {
            if (World.IsAlive(projectile))
                World.Destroy(projectile);
        }
        
        _projectilesToDestroy.Clear();
    }
}
