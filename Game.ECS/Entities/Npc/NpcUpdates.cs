using Arch.Core;
using Game.ECS.Schema.Components;
using Game.ECS.Schema.Templates;

namespace Game.ECS.Entities.Npc;

public static class NpcUpdate
{
    public static void ApplyNpcUpdate(this World world, Entity entity, NpcTemplate template)
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
        
        uniqueId.Value = template.Id;
        networkId.Value = template.IdentityTemplate.NetworkId;
        mapId.Value = template.LocationTemplate.MapId;
        genderId.Value = (byte)template.IdentityTemplate.Gender;
        vocationId.Value = (byte)template.IdentityTemplate.Vocation;
        position.X = template.LocationTemplate.X;
        position.Y = template.LocationTemplate.Y;
        floor.Value = (sbyte)template.LocationTemplate.Floor;
        direction.X = template.DirectionTemplate.DirX;
        direction.Y = template.DirectionTemplate.DirY;
        health.Current = template.VitalsTemplate.CurrentHp;
        health.Max = template.VitalsTemplate.MaxHp;
        mana.Current = template.VitalsTemplate.CurrentMp;
        mana.Max = template.VitalsTemplate.MaxMp;
    }
    
    /// <summary>
    /// Aplica um snapshot de estado (movimento) a um NPC.
    /// </summary>
    public static void ApplyNpcState(this World world, Entity entity, NpcStateSnapshot state)
    {
        ref var position = ref world.Get<Position>(entity);
        ref var floor = ref world.Get<Floor>(entity);
        ref var speed = ref world.Get<Speed>(entity);
        ref var direction = ref world.Get<Direction>(entity);
        
        position.X = state.X;
        position.Y = state.Y;
        floor.Value = state.Floor;
        speed.Value = state.Speed;
        direction.X = state.DirectionX;
        direction.Y = state.DirectionY;
    }
    
    /// <summary>
    /// Aplica um snapshot de vitals (HP/MP) a um NPC.
    /// </summary>
    public static void ApplyNpcVitals(this World world, Entity entity, NpcVitalsSnapshot vitals)
    {
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        
        health.Current = vitals.CurrentHp;
        health.Max = vitals.MaxHp;
        mana.Current = vitals.CurrentMp;
        mana.Max = vitals.MaxMp;
    }
}