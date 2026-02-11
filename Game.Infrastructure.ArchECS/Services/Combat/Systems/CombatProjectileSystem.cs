using Arch.Bus;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.Combat.Events;
using Game.Infrastructure.ArchECS.Services.EntityRegistry;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;

namespace Game.Infrastructure.ArchECS.Services.Combat.Systems;

/// <summary>
/// Atualiza projéteis e aplica dano ao colidir.
/// </summary>
public sealed partial class CombatProjectileSystem : GameSystem
{
    private readonly WorldMap _map;

    public CombatProjectileSystem(World world, WorldMap map) : base(world)
    {
        _map = map;
        Hook();
    }

    public override void Dispose()
    {
        Unhook();
        base.Dispose();
    }

    [Event(order: 1)]
    public void HandleProjectileCreation(ref ProjectileSpawnedEvent evt)
    {
        var attackerId = Registry.GetExternalId(evt.Attacker, EntityDomain.Combat);
        var speedCellsPerTick = Math.Max(0f, evt.Speed) / SimulationConfig.TicksPerSecond;

        World.Create(
            new Position { X = evt.PosX, Y = evt.PosY },
            new FloorId { Value = evt.Floor },
            new Projectile
            {
                OwnerId = attackerId,
                OwnerTeamId = evt.TeamId,
                Damage = evt.Damage,
                DirX = evt.DirX,
                DirY = evt.DirY,
                RemainingRange = evt.Range,
                SpeedCellsPerTick = speedCellsPerTick,
                TravelRemainder = 0f
            });
    }
    
    
    [Query]
    [All<Projectile, Position, FloorId>]
    private void TickProjectile(
        in Entity entity,
        ref Projectile projectile,
        ref Position position,
        ref FloorId floor)
    {
        var totalTravelDistance = projectile.SpeedCellsPerTick + projectile.TravelRemainder;
        var cellsToMove = (int)totalTravelDistance;
        projectile.TravelRemainder = totalTravelDistance - cellsToMove;

        if (cellsToMove <= 0)
            return;

        for (var i = 0; i < cellsToMove; i++)
        {
            var nextX = position.X + projectile.DirX;
            var nextY = position.Y + projectile.DirY;

            if (!_map.InBounds(nextX, nextY, floor.Value) || _map.IsBlocked(nextX, nextY, floor.Value))
            {
                World.Destroy(entity);
                return;
            }

            position.X = nextX;
            position.Y = nextY;
            projectile.RemainingRange--;

            if (!_map.TryGetFirstEntity(position, floor.Value, out var targetEntity) || targetEntity == Entity.Null)
            {
                if (projectile.RemainingRange <= 0)
                {
                    World.Destroy(entity);
                    return;
                }

                continue;
            }

            if (Registry.TryGetEntity(projectile.OwnerId, EntityDomain.Combat, out var ownerEntity) && targetEntity == ownerEntity)
                continue;

            TryApplyProjectileDamage(projectile, targetEntity, projectile.DirX, projectile.DirY);
            World.Destroy(entity);
            return;
        }
    }

    private void TryApplyProjectileDamage(Projectile projectile, Entity targetEntity, int dirX, int dirY)
    {
        if (!World.Has<CombatStats>(targetEntity))
            return;

        if (World.Has<CharacterId>(targetEntity) &&
            (_map.Flags & MapFlags.PvPEnabled) == 0)
            return;

        var targetTeam = World.Has<TeamId>(targetEntity) ? World.Get<TeamId>(targetEntity).Value : 0;
        if (projectile.OwnerTeamId != 0 && projectile.OwnerTeamId == targetTeam)
            return;

        ref var targetStats = ref World.Get<CombatStats>(targetEntity);
        if (targetStats.CurrentHealth <= 0)
            return;

        var damage = projectile.Damage;
        if (damage <= 0)
            return;

        targetStats.CurrentHealth = Math.Max(0, targetStats.CurrentHealth - damage);

        var targetPos = World.Has<Position>(targetEntity) ? World.Get<Position>(targetEntity) : new Position();
        var targetFloor = World.Has<FloorId>(targetEntity) ? World.Get<FloorId>(targetEntity).Value : 0;

        if (!Registry.TryGetEntity(projectile.OwnerId, EntityDomain.Combat, out var attackerEntity))
            attackerEntity = Entity.Null;

        CombatDamageEvent evt = new CombatDamageEvent(
            attackerEntity,
            targetEntity,
            damage,
            dirX,
            dirY,
            targetPos.X,
            targetPos.Y,
            targetFloor);
        
        EventBus.Send(ref evt);
    }
}
