using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services.Snapshot.Data;

namespace Game.ECS.Services.Snapshot;

public static class NpcSnapshotService
{
    public static NpcData BuildNpcSnapshot(this World world, Entity entity, int mapId, string name)
    {
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var npcId = ref world.Get<UniqueID>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var facing = ref world.Get<Direction>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        ref var combatStats = ref world.Get<CombatStats>(entity);
        ref var aiBehavior = ref world.Get<AIBehaviour>(entity);

        return new NpcData(
            NpcId: npcId.Value,
            NetworkId: networkId.Value,
            MapId: mapId,
            Name: name,
            X: position.X,
            Y: position.Y,
            Z: position.Z,
            DirX: facing.X,
            DirY: facing.Y,
            Hp: health.Current,
            MaxHp: health.Max,
            HpRegen: health.RegenerationRate,
            Mp: mana.Current,
            MaxMp: mana.Max,
            MpRegen: mana.RegenerationRate,
            PhysicalAttack: combatStats.AttackPower,
            MagicAttack: combatStats.MagicPower,
            PhysicalDefense: combatStats.Defense,
            MagicDefense: combatStats.MagicDefense,
            BehaviorType: aiBehavior.Type,
            VisionRange: aiBehavior.VisionRange,
            AttackRange: aiBehavior.AttackRange,
            LeashRange: aiBehavior.LeashRange,
            PatrolRadius: aiBehavior.PatrolRadius,
            IdleDurationMin: aiBehavior.IdleDurationMin,
            IdleDurationMax: aiBehavior.IdleDurationMax
        );
    }
}