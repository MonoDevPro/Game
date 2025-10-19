using System;
using Arch.Core;
using Arch.System;
using Game.Core.MapGame.Services;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using GodotClient.Game.Simulation.Systems;
using Microsoft.Extensions.DependencyInjection;

namespace GodotClient.Game.Simulation;

public sealed class ClientSimulation(IServiceProvider provider) : GameSimulation
{
    public override void ConfigureSystems(World world, Group<float> group)
    {
        ISystem<float>[] systems =
        [
            new ClientInputSystem(world),
            new MovementSystem(world),
            new AnimationStateSystem(world),
            new SpritePlaybackSystem(world),
            new YSortSystem(world),
            new RemoteInterpolationSystem(world),
            new VisualLifecycleSystem(world),

            new NetworkDirtyMarkingSystem(world),
            new NetworkSenderSystem(world, provider.GetRequiredService<INetworkManager>()),

            // Se ainda quiser atualizar Node2D por Position brute-force:
            // new ClientRenderSystem(world, _gameScript)
        ];
        group.Add(systems);
    }
    
    public void ReceivePlayer(in Entity e, in PlayerSnapshot snap)
    {
        ref var position = ref World.Get<Position>(e);
        position.X = snap.PositionX;
        position.Y = snap.PositionY;
        position.Z = snap.PositionZ;
        
        ref var facing = ref World.Get<Facing>(e);
        facing.DirectionX = snap.FacingX;
        facing.DirectionY = snap.FacingY;
        
        ref var velocity = ref World.Get<Velocity>(e);
        velocity.Speed = snap.Speed;
    }
    
    // Chame isso ao receber um snapshot do servidor
    public void ReceivePlayerState(in Entity e, in PlayerStateSnapshot snap)
    {
        ref var position = ref World.Get<Position>(e);
        position.X = snap.PositionX;
        position.Y = snap.PositionY;
        position.Z = snap.PositionZ;
        
        ref var facing = ref World.Get<Facing>(e);
        facing.DirectionX = snap.FacingX;
        facing.DirectionY = snap.FacingY;
        
        ref var velocity = ref World.Get<Velocity>(e);
        velocity.Speed = snap.Speed;
    }
    
    public bool ApplyPlayerVitals(in Entity e, in PlayerVitalsSnapshot snap)
    {
        bool updated = false;

        ref var health = ref World.Get<Health>(e);
        if (health.Current != snap.CurrentHp || health.Max != snap.MaxHp)
        {
            health.Current = snap.CurrentHp;
            health.Max = snap.MaxHp;
            updated = true;
        }
        ref var mana = ref World.Get<Mana>(e);
        if (mana.Current != snap.CurrentMp || mana.Max != snap.MaxMp)
        {
            mana.Current = snap.CurrentMp;
            mana.Max = snap.MaxMp;
            updated = true;
        }
        return updated;
    }
    
    public bool ApplyStateFromServer(Entity entity, PlayerStateSnapshot state)
    {
        bool updated = false;

        ref var facing = ref World.Get<Facing>(entity);
        ref var position = ref World.Get<Position>(entity);
        ref var velocity = ref World.Get<Velocity>(entity);
        
        if (position.X != state.PositionX || 
            position.Y != state.PositionY || 
            position.Z != state.PositionZ)
        {
            position.X = state.PositionX;
            position.Y = state.PositionY;
            position.Z = state.PositionZ;
            updated = true;
        }

        if (facing.DirectionX != state.FacingX || 
            facing.DirectionY != state.FacingY)
        {
            facing.DirectionX = state.FacingX;
            facing.DirectionY = state.FacingY;
            updated = true;
        }

        if (Math.Abs(velocity.Speed - state.Speed) > 0.01f)
        {
            velocity.Speed = state.Speed;
            updated = true;
        }

        return updated;
    }
}
