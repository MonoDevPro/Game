using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.Server.Factories;
using Simulation.Core.Server.Persistence.Contracts;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Templates;
using Simulation.Core.Shared.Utils.Map;

namespace Simulation.Core.Server.Systems;

public sealed partial class EntityFactorySystem(World world): BaseSystem<World, float>(world)
{
    [Query]
    [All<NewlyCreated, MapData>]
    private void CreateMap(in Entity entity, ref MapData data)
    {
        World.Add<MapId>(entity, new MapId { Value = data.MapId });
        World.Add<MapInfo>(entity, new MapInfo { Name = data.Name, Width = data.Width, Height = data.Height });
        World.Add<MapService>(entity, MapService.CreateFromTemplate(data));
        
        World.Remove<MapData>(entity);
    }
    
    [Query]
    [All<NewlyCreated, PlayerData>]
    private void CreatePlayer(in Entity entity, ref PlayerData data)
    {
        World.Add(entity,
            new PlayerId { Value = data.Id },
            new PlayerInfo { Name = data.Name, Gender = data.Gender, Vocation = data.Vocation },
            new MapId { Value = data.MapId },
            new Position { X = data.PosX, Y = data.PosY },
            new Direction { X = data.DirX, Y = data.DirY },
            new AttackStats { CastTime = data.AttackCastTime, Cooldown = data.AttackCooldown, Damage = data.AttackDamage, AttackRange = data.AttackRange },
            new MoveStats { Speed = data.MoveSpeed },
            new Health { Current = data.HealthCurrent, Max = data.HealthMax },
            new StateComponent { Value = StateFlags.Idle }
        );
        
        World.Remove<PlayerData>(entity);
    }

    
}