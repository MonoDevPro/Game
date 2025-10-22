using System;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.ECS.Components;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema responsável por coletar componentes marcados como dirty e emitir atualizações de estado.
/// </summary>
public sealed partial class SyncSystem(World world, GameEventSystem events, EntityFactory factory)
    : GameSystem(world, events, factory)
{
    [Query]
    [All<NetworkId, DirtyFlags>]
    private void SyncDirtyEntities(
        in Entity entity,
        in NetworkId networkId,
        ref DirtyFlags dirty,
        [Data] float deltaTime)
    {
        if (dirty.Flags == 0)
            return;

        if (dirty.IsDirty(DirtyComponentType.Position) && World.TryGet(entity, out Position position))
        {
            SendPositionUpdate(networkId.Value, in position);
        }

        if (dirty.IsDirty(DirtyComponentType.Health) && World.TryGet(entity, out Health health))
        {
            SendHealthUpdate(networkId.Value, in health);
        }

        if (dirty.IsDirty(DirtyComponentType.Mana) && World.TryGet(entity, out Mana mana))
        {
            SendManaUpdate(networkId.Value, in mana);
        }

        if (dirty.IsDirty(DirtyComponentType.Facing) && World.TryGet(entity, out Facing facing))
        {
            SendFacingUpdate(networkId.Value, in facing);
        }

        if (dirty.IsDirty(DirtyComponentType.Combat) && World.TryGet(entity, out CombatState combat))
        {
            SendCombatUpdate(networkId.Value, in combat);
        }

        dirty.ClearAll();
    }

    private void SendPositionUpdate(int networkId, in Position position)
    {
        Console.WriteLine($"[SYNC] Entity {networkId} moved to ({position.X}, {position.Y})");
    }

    private void SendHealthUpdate(int networkId, in Health health)
    {
        Console.WriteLine($"[SYNC] Entity {networkId} health: {health.Current}/{health.Max}");
    }

    private void SendManaUpdate(int networkId, in Mana mana)
    {
        Console.WriteLine($"[SYNC] Entity {networkId} mana: {mana.Current}/{mana.Max}");
    }

    private void SendFacingUpdate(int networkId, in Facing facing)
    {
        Console.WriteLine($"[SYNC] Entity {networkId} facing: ({facing.DirectionX}, {facing.DirectionY})");
    }

    private void SendCombatUpdate(int networkId, in CombatState combat)
    {
        Console.WriteLine($"[SYNC] Entity {networkId} combat target: {combat.TargetNetworkId}");
    }
}
