using System;
using Arch.Core;
using Arch.System;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.DTOs;
using Game.ECS.Systems;
using Game.Network.Abstractions;
using Godot;
using GodotClient.Autoloads;
using GodotClient.ECS;
using GodotClient.ECS.Components;
using GodotClient.Simulation.Players;
using GodotClient.Simulation.Systems;
using Microsoft.Extensions.DependencyInjection;
using ClientInputSystem = GodotClient.ECS.Systems.ClientInputSystem;
using NetworkDirtyMarkingSystem = GodotClient.ECS.Systems.NetworkDirtyMarkingSystem;
using NetworkSenderSystem = GodotClient.ECS.Systems.NetworkSenderSystem;
using RemoteInterpolationSystem = GodotClient.ECS.Systems.RemoteInterpolationSystem;

namespace GodotClient.Simulation;

public record PlayerData(int NetworkId, Entity Entity, PlayerVisual Visual);

public sealed class ClientSimulation(IServiceProvider provider) : GameSimulation
{
    private readonly PlayerIndexService _playerIndexService = new();
    
    public override void ConfigureSystems(World world, Group<float> group)
    {
        ISystem<float>[] systems =
        [
            new ClientInputSystem(world),
            new MovementSystem(world),
            new RemoteInterpolationSystem(world),

            new NetworkDirtyMarkingSystem(world),
            new NetworkSenderSystem(world, provider.GetRequiredService<INetworkManager>()),

            // Se ainda quiser atualizar Node2D por Position brute-force:
            // new ClientRenderSystem(world, _gameScript)
        ];
        group.Add(systems);
    }
    
    public PlayerData SpawnPlayer(PlayerSnapshot snapshot)
    {
        if (_playerIndexService.TryGetPlayer(snapshot.NetworkId, out var playerData))
        {
            _playerIndexService.UnregisterPlayer(snapshot.NetworkId);
            DespawnEntity(playerData!.Entity);
            playerData.Visual.QueueFree();
        }

        bool isLocal = false;
        if (NetworkClient.Instance.TryGetLocalPlayerNetworkId(out var localNetworkId))
            isLocal = localNetworkId == snapshot.NetworkId;
        
        var world = provider.GetRequiredService<SceneTree>().Root.GetNode<Node2D>("/root/Game/World");
        var playerVisual = new PlayerVisual
        {
            Name = $"Player_{snapshot.NetworkId}"
        };
        world.AddChild(playerVisual);
        playerVisual.UpdateFromSnapshot(snapshot, isLocal);
        
        var spawnData = new PlayerSpawnData(
            snapshot.PlayerId,
            snapshot.NetworkId,
            snapshot.PositionX,
            snapshot.PositionY,
            snapshot.PositionZ,
            snapshot.FacingX,
            snapshot.FacingY,
            snapshot.Hp,
            snapshot.MaxHp,
            snapshot.HpRegen,
            snapshot.Mp,
            snapshot.MaxMp,
            snapshot.MpRegen,
            (float)snapshot.MovementSpeed,
            (float)snapshot.AttackSpeed,
            snapshot.PhysicalAttack,
            snapshot.MagicAttack,
            snapshot.PhysicalDefense,
            snapshot.MagicDefense
        );
        var entity = base.SpawnPlayer(spawnData);
        World.Add<RemoteInterpolation, NodeRef>(entity,
            new RemoteInterpolation(),
            new NodeRef { IsVisible = true, Node2D = playerVisual }
        );
        if (isLocal)
        {
            World.Add<LocalPlayerTag>(entity);
            playerVisual.Modulate = new Color(0.8f, 1f, 0.8f); // ligeiramente verde
        }
        
        var newPlayerData = new PlayerData(snapshot.NetworkId, entity, playerVisual);
        _playerIndexService.RegisterPlayer(newPlayerData);
        return newPlayerData;
    }
    
    public void DespawnPlayer(in PlayerDespawn despawn)
    {
        if (_playerIndexService.TryGetPlayer(despawn.NetworkId, out var playerData))
        {
            DespawnEntity(playerData!.Entity);
            playerData.Visual.QueueFree();
            _playerIndexService.UnregisterPlayer(despawn.NetworkId);
        }
    }

    public bool ApplyRemotePlayerInput(in PlayerInputSnapshot input)
    {
        if (!_playerIndexService.TryGetPlayer(input.NetworkId, out var playerData)) 
            return false;
        
        base.TryApplyPlayerInput(playerData!.Entity, input.Input);
        return true;
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
    
    public bool TryGetPlayerEntity(int networkId, out Entity entity)
    {
        if (_playerIndexService.TryGetPlayer(networkId, out var playerData))
        {
            entity = playerData!.Entity;
            return true;
        }
        entity = default;
        return false;
    }
    
    public void ClearSimulation()
    {
        foreach (var player in _playerIndexService.GetAllPlayers())
        {
            DespawnEntity(player.Entity);
            player.Visual.QueueFree();
        }
        _playerIndexService.Clear();
    }
}