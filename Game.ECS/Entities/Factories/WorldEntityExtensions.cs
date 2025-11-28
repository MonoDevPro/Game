using Arch.Core;
using Arch.LowLevel;
using Game.Domain.Templates;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Services;

namespace Game.ECS.Entities.Factories;

/// <summary>
/// Extension methods for creating entities in the ECS World.
/// Provides convenient factory methods for Player, NPC, and other entity types.
/// </summary>
public static class WorldEntityExtensions
{
    /// <summary>
    /// Creates a player entity from a snapshot.
    /// </summary>
    public static Entity CreatePlayer(this World world, Func<string, Handle<string>> resources, in PlayerSnapshot snapshot)
    {
        var template = new PlayerTemplate(
            PlayerId: snapshot.PlayerId,
            NetworkId: snapshot.NetworkId,
            MapId: snapshot.MapId,
            Name: snapshot.Name,
            GenderId: snapshot.GenderId,
            VocationId: snapshot.VocationId,
            PosX: snapshot.PosX,
            PosY: snapshot.PosY,
            Floor: snapshot.Floor,
            DirX: snapshot.DirX,
            DirY: snapshot.DirY,
            Hp: snapshot.Hp,
            MaxHp: snapshot.MaxHp,
            HpRegen: 0f, // Default regen, not in snapshot
            Mp: snapshot.Mp,
            MaxMp: snapshot.MaxMp,
            MpRegen: 0f, // Default regen, not in snapshot
            MovementSpeed: snapshot.MovementSpeed,
            AttackSpeed: snapshot.AttackSpeed,
            PhysicalAttack: snapshot.PhysicalAttack,
            MagicAttack: snapshot.MagicAttack,
            PhysicalDefense: snapshot.PhysicalDefense,
            MagicDefense: snapshot.MagicDefense
        );
        
        return PlayerLifecycle.CreatePlayer(world, resources, template);
    }
    
    /// <summary>
    /// Creates an NPC entity from a snapshot.
    /// </summary>
    public static Entity CreateNPC(this World world, Func<string, Handle<string>> resources, in NpcSnapshot snapshot)
    {
        // Create a minimal template from snapshot data
        var template = new NpcTemplate
        {
            Id = $"npc_{snapshot.NetworkId}",
            Name = snapshot.Name,
            BaseHp = snapshot.MaxHp,
            BaseMp = snapshot.MaxMp,
            Stats = new NpcStats
            {
                MovementSpeed = 1f, // Default, not in snapshot
                PhysicalAttack = 10, // Default
                MagicAttack = 5, // Default
                PhysicalDefense = 5, // Default
                MagicDefense = 5, // Default
                AttackSpeed = 1f, // Default
                HpRegen = 0f,
                MpRegen = 0f
            },
            Behavior = new NpcBehaviorConfig
            {
                VisionRange = 8f,
                AttackRange = 1.5f,
                LeashRange = 20f,
                PatrolRadius = 5f,
                IdleDurationMin = 1f,
                IdleDurationMax = 3f
            }
        };
        
        var entity = NpcLifecycle.CreateNPC(
            world, 
            resources, 
            template, 
            new Position(snapshot.X, snapshot.Y), 
            snapshot.Floor, 
            snapshot.MapId, 
            snapshot.NetworkId);
            
        // Apply current HP/MP from snapshot
        ref var health = ref world.Get<Health>(entity);
        ref var mana = ref world.Get<Mana>(entity);
        health.Current = snapshot.Hp;
        mana.Current = snapshot.Mp;
        
        // Apply direction from snapshot
        ref var direction = ref world.Get<Direction>(entity);
        direction.DirectionX = snapshot.DirX;
        direction.DirectionY = snapshot.DirY;
        
        return entity;
    }
    
    /// <summary>
    /// Creates an NPC entity from snapshot data with default string handling.
    /// </summary>
    public static Entity CreateNPC(this World world, in NpcSnapshot snapshot)
    {
        return world.CreateNPC(_ => default, in snapshot);
    }
    
    /// <summary>
    /// Sets the position of an entity.
    /// </summary>
    public static void SetPosition(this World world, Entity entity, Position position)
    {
        ref var pos = ref world.Get<Position>(entity);
        pos = position;
    }
}
