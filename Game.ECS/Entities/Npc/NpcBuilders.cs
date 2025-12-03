using Arch.Core;
using Game.Domain.Enums;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;
using Game.ECS.Services.Index;

namespace Game.ECS.Entities.Npc;

public static class NpcBuilder
{
    public static NpcTemplate BuildNpcTemplate(this World world, Entity entity, ResourceIndex<string> resources, NpcTemplate? existingTemplate = null)
    {
        ref var uniqueId = ref world.Get<UniqueID>(entity);
        ref var networkId = ref world.Get<NetworkId>(entity);
        ref var mapId = ref world.Get<MapId>(entity);
        ref var name = ref world.Get<NameHandle>(entity);
        ref var genderId = ref world.Get<GenderId>(entity);
        ref var vocationId = ref world.Get<VocationId>(entity);
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        
        existingTemplate ??= new NpcTemplate();
        existingTemplate.Id = uniqueId.Value;
        existingTemplate.IdentityTemplate = new IdentityTemplate(
            NetworkId: networkId.Value,
            Name: resources.Get(name.Value),
            Gender: (Gender)genderId.Value,
            Vocation: (VocationType)vocationId.Value);
        existingTemplate.LocationTemplate = new LocationTemplate(
            MapId: mapId.Value,
            Floor: floor.Value,
            X: position.X,
            Y: position.Y);
        existingTemplate.StatsTemplate = new StatsTemplate(
            MovementSpeed: 1f,
            AttackSpeed: 1f,
            PhysicalAttack: 10,
            MagicAttack: 5,
            PhysicalDefense: 5,
            MagicDefense: 3);
        existingTemplate.VitalsTemplate = new VitalsTemplate(
            CurrentHp: health.Current,
            MaxHp: health.Max,
            CurrentMp: mana.Current,
            MaxMp: mana.Max,
            HpRegen: 0.1f,
            MpRegen: 0.1f);
        existingTemplate.BehaviorTemplate = new BehaviorTemplate(
            BehaviorType: BehaviorType.Passive,
            VisionRange: 5f,
            AttackRange: 1f,
            LeashRange: 10f,
            PatrolRadius: 3f,
            IdleDurationMin: 2f,
            IdleDurationMax: 5f);
        existingTemplate.DirectionTemplate = new DirectionTemplate(
            DirX: direction.X,
            DirY: direction.Y);
        return existingTemplate;
    }
}