using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Shared;
using Simulation.Core.ECS.Shared.Utils.Map;
using Simulation.Core.Models;

namespace Simulation.Core.ECS.Server.Systems;

public sealed partial class EntityFactorySystem(World world): BaseSystem<World, float>(world)
{
    [Query]
    [All<NewlyCreated, MapModel>]
    private void CreateMap(in Entity entity, ref MapModel model)
    {
        World.Add<MapId>(entity, new MapId { Value = model.MapId });
        World.Add<MapInfo>(entity, new MapInfo { Name = model.Name, Width = model.Width, Height = model.Height });
        // Remove o MapData para liberar memória
        World.Remove<MapModel>(entity);
        
        // Service deve ser criado após MapId e MapInfo
        World.Add<MapService>(entity, MapService.CreateFromTemplate(model));
    }
    
    [Query]
    [All<NewlyCreated, PlayerModel>]
    private void CreatePlayer(in Entity entity, ref PlayerModel model)
    {
        World.Add(entity,
            new PlayerId { Value = model.Id },
            new PlayerInfo { Name = model.Name, Gender = model.Gender, Vocation = model.Vocation },
            new MapId { Value = model.MapId },
            new Position { X = model.PosX, Y = model.PosY },
            new Direction { X = model.DirX, Y = model.DirY },
            new AttackStats { CastTime = model.AttackCastTime, Cooldown = model.AttackCooldown, Damage = model.AttackDamage, AttackRange = model.AttackRange },
            new MoveStats { Speed = model.MoveSpeed },
            new Health { Current = model.HealthCurrent, Max = model.HealthMax },
            new StateComponent { Value = StateFlags.Idle }
        );
        
        World.Remove<PlayerModel>(entity);
    }

    
}