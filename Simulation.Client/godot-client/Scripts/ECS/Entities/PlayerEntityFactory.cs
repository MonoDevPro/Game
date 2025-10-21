using Arch.Core;
using Game.ECS.Components;
using Game.ECS.DTOs;
using Game.ECS.Entities.Archetypes;
using Godot;
using GodotClient.ECS.Components;

namespace GodotClient.ECS.Entities;

public class PlayerEntityFactory : IEntityFactory
{
    private readonly World _world;

    public PlayerEntityFactory(World world)
    {
        _world = world;
    }

    public Entity CreatePlayer(int networkId, PlayerSpawnData data)
    {
        var entity = _world.Create(GameArchetypes.PlayerCharacter);
        var components = new object[]
        {
            new NetworkId { Value = data.NetworkId },
            new PlayerId { Value = data.PlayerId },
            new Position { X = data.SpawnX, Y = data.SpawnY, Z = data.SpawnZ },
            new Facing { DirectionX = data.FacingX, DirectionY = data.FacingY },
            new Velocity { DirectionX = 0, DirectionY = 0, Speed = 0f },
            new Movement { Timer = 0f },
            new Health { Current = data.Hp, Max = data.MaxHp, RegenerationRate = data.HpRegen },
            new Mana { Current = data.Mp, Max = data.MaxMp, RegenerationRate = data.MpRegen },
            new Walkable { BaseSpeed = 5f, CurrentModifier = data.MovementSpeed },
            new Attackable { BaseSpeed = 1f, CurrentModifier = data.AttackSpeed },
            new AttackPower { Physical = data.PhysicalAttack, Magical = data.MagicAttack },
            new Defense { Physical = data.PhysicalDefense, Magical = data.MagicDefense },
            new CombatState { },
            new NetworkDirty { Flags = SyncFlags.All },
            new PlayerInput(),
            new PlayerControlled()
        };
        _world.SetRange(entity, components);
        return entity;
    }

    public Entity CreateRemotePlayer(int networkId, PlayerSnapshot snapshot, Node2D visual)
    {
        var entity = CreatePlayer(networkId, snapshot.ToSpawnData());
        _world.Add(entity,
            new RemoteInterpolation { LerpAlpha = 0.15f, ThresholdPx = 1f },
            new NodeRef { Node2D = visual, IsVisible = true }
        );
        return entity;
    }

    public Entity CreateLocalPlayer(int networkId, PlayerSnapshot snapshot, Node2D visual);
    {
        var entity = CreateRemotePlayer(networkId, name, spawnPos, visual);
        _world.Add<LocalPlayerTag>(entity);
        return entity;
    }
}