using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.EntityRegistry;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;

namespace Game.Infrastructure.ArchECS.Services.Combat.Systems;

/// <summary>
/// Atualiza projéteis e aplica dano ao colidir.
/// </summary>
public sealed partial class CombatProjectileSystem(
    World world,
    WorldMap map,
    CombatEventBuffer events)
    : GameSystem(world)
{
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

            if (!map.InBounds(nextX, nextY, floor.Value) || map.IsBlocked(nextX, nextY, floor.Value))
            {
                World.Destroy(entity);
                return;
            }

            position.X = nextX;
            position.Y = nextY;
            projectile.RemainingRange--;

            if (projectile.RemainingRange <= 0)
            {
                World.Destroy(entity);
                return;
            }

            if (!map.TryGetFirstEntity(position, floor.Value, out var targetEntity) || targetEntity == Entity.Null)
                continue;

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
            (map.Flags & MapFlags.PvPEnabled) == 0)
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

        var targetId = Registry.GetExternalId(targetEntity, EntityDomain.Combat);
        var targetPos = World.Has<Position>(targetEntity) ? World.Get<Position>(targetEntity) : new Position();
        var targetFloor = World.Has<FloorId>(targetEntity) ? World.Get<FloorId>(targetEntity).Value : 0;
        events.Add(new CombatEvent(
            CombatEventType.Hit,
            projectile.OwnerId,
            targetId,
            dirX,
            dirY,
            damage,
            targetPos.X,
            targetPos.Y,
            targetFloor,
            0f,
            0));
    }
}
