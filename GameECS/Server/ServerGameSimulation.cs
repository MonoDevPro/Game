using Arch.Core;
using Game.Domain.AOI.ValueObjects;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Commons.Entities;
using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Attributes;
using Game.Domain.Commons.ValueObjects.Character;
using Game.Domain.Commons.ValueObjects.Equipment;
using Game.Domain.Commons.ValueObjects.Identitys;
using Game.Domain.Commons.ValueObjects.Map;
using Game.Domain.Commons.ValueObjects.Vitals;
using Game.Domain.Navigation;
using Game.Domain.Navigation.Core;
using Game.Domain.Navigation.ValueObjects;
using Game.Domain.Player;
using Game.Domain.Player.ValueObjects;
using Game.Domain.Vocations;
using Game.Domain.Vocations.ValueObjects;
using GameECS.Core;
using GameECS.Modules.Navigation.Shared.Data;
using GameECS.Server.Entities.Components;
using GameECS.Server.Entities.Systems;
using GameECS.Systems;

namespace GameECS.Server;

/// <summary>
/// Simulacao ECS do servidor (mundo + sistemas).
/// </summary>
public sealed class ServerGameSimulation : IDisposable
{
    private const int TargetTicksPerSecond = 60;
    private const float FixedDelta = 1f / TargetTicksPerSecond;

    private readonly Dictionary<int, Entity> _entitiesByNetworkId = new();
    private readonly EntityFactory _entityFactory;
    private readonly AOIManager _aoiManager;
    private readonly NavigationGrid _navigationGrid;
    private readonly MovementSystem _movementSystem;
    private readonly NpcAISystem _npcAiSystem;
    private readonly AggroSystem _aggroSystem;
    private readonly PetBehaviorSystem _petSystem;
    private readonly AOIUpdateSystem _aoiSystem;
    private readonly NpcRespawnSystem _npcRespawnSystem;
    private float _accumulator;
    private long _tick;

    public ServerGameSimulation(Map map)
    {
        World = World.Create();
        _entityFactory = new EntityFactory(World);
        _aoiManager = new AOIManager(World);
        _navigationGrid = BuildNavigationGrid(map);

        _movementSystem = new MovementSystem(World, _navigationGrid);
        _npcAiSystem = new NpcAISystem(World);
        _aggroSystem = new AggroSystem(World);
        _petSystem = new PetBehaviorSystem(World);
        _aoiSystem = new AOIUpdateSystem(World, _aoiManager);
        _npcRespawnSystem = new NpcRespawnSystem(World);
    }

    public World World { get; }
    public long Tick => _tick;

    public void Update(float deltaTime)
    {
        _accumulator += Math.Max(0, deltaTime);

        while (_accumulator >= FixedDelta)
        {
            _tick++;
            UpdateTick(_tick);
            _accumulator -= FixedDelta;
        }
    }

    public Entity CreatePlayerEntity(PlayerSpawnData data)
    {
        var attributes = BuildPlayerAttributes(data);
        var entity = _entityFactory.CreatePlayer(attributes);

        World.Add(entity, new NetworkId(data.NetworkId));
        World.Add(entity, new NavigationAgent());
        World.Add(entity, AgentConfig.Default);
        World.Add(entity, new MovementAction());
        World.Add(entity, new MovementInput());
        World.Add(entity, new GridPathBuffer());
        World.Add(entity, new PathState());
        World.Add(entity, new AreaOfInterest(attributes.VisibilityConfig.ViewRadius));

        ref var identity = ref World.Get<Identity>(entity);
        ref var position = ref World.Get<GridPosition>(entity);
        _navigationGrid.TryOccupy(position, identity.UniqueId);

        _entitiesByNetworkId[data.NetworkId] = entity;
        return entity;
    }

    public Entity CreateNpcEntity(string templateId, int x, int y)
    {
        var entity = _entityFactory.CreateNpc(templateId, x, y);
        World.Add(entity, new NavigationAgent());
        World.Add(entity, AgentConfig.Default);
        World.Add(entity, new MovementAction());
        World.Add(entity, new MovementInput());
        World.Add(entity, new GridPathBuffer());
        World.Add(entity, new PathState());

        ref var identity = ref World.Get<Identity>(entity);
        ref var position = ref World.Get<GridPosition>(entity);
        _navigationGrid.TryOccupy(position, identity.UniqueId);

        return entity;
    }

    public bool ApplyPlayerInput(int networkId, in MoveInputData input)
    {
        if (!_entitiesByNetworkId.TryGetValue(networkId, out var entity))
            return false;

        if (!World.IsAlive(entity))
            return false;

        if (!_navigationGrid.IsValidCoord(input.TargetX, input.TargetY))
            return false;

        if (!_navigationGrid.IsWalkable(input.TargetX, input.TargetY))
            return false;

        ref var movementInput = ref World.Get<MovementInput>(entity);
        movementInput.Set(input.TargetX, input.TargetY);

        _movementSystem.TryStartMovement(entity, _tick);
        return true;
    }

    public bool TryGetEntity(int networkId, out Entity entity)
        => _entitiesByNetworkId.TryGetValue(networkId, out entity);

    public MovementSnapshot GetMovementSnapshot(Entity entity)
    {
        ref var position = ref World.Get<GridPosition>(entity);
        ref var movement = ref World.Get<MovementAction>(entity);
        ref var input = ref World.Get<MovementInput>(entity);

        var targetX = movement.IsMoving ? movement.TargetCell.X : (input.HasInput ? input.TargetX : position.X);
        var targetY = movement.IsMoving ? movement.TargetCell.Y : (input.HasInput ? input.TargetY : position.Y);
        var direction = movement.IsMoving
            ? (byte)movement.Direction
            : (byte)DirectionExtensions.FromPositions(position, new GridPosition(targetX, targetY));

        return new MovementSnapshot
        {
            NetworkId = World.TryGet<NetworkId>(entity, out var networkId) ? networkId.Value : 0,
            X = position.X,
            Y = position.Y,
            TargetX = targetX,
            TargetY = targetY,
            Direction = direction,
            IsMoving = movement.IsMoving,
            StartTick = movement.StartTick,
            EndTick = movement.EndTick
        };
    }

    public void DestroyPlayer(int networkId)
    {
        if (!_entitiesByNetworkId.TryGetValue(networkId, out var entity))
            return;

        if (World.IsAlive(entity))
        {
            if (World.TryGet<Identity>(entity, out var identity) &&
                World.TryGet<GridPosition>(entity, out var position))
            {
                _navigationGrid.Release(position, identity.UniqueId);
                _aoiManager.RemoveEntity(identity.UniqueId);
            }

            World.Destroy(entity);
        }

        _entitiesByNetworkId.Remove(networkId);
    }

    private void UpdateTick(long tick)
    {
        _movementSystem.Update(tick);
        _npcAiSystem.Update(tick);
        _aggroSystem.Update(tick);
        _petSystem.Update(tick);
        _npcRespawnSystem.Update(tick);
        _aoiSystem.Update(tick);
    }

    private static NavigationGrid BuildNavigationGrid(Map map)
    {
        var grid = new NavigationGrid(map.Width, map.Height, 1f);
        var collision = map.GetCollisionLayer(0);

        for (int i = 0; i < collision.Length; i++)
        {
            int x = i % map.Width;
            int y = i / map.Width;
            grid.SetWalkable(x, y, collision[i] == 0);
        }

        return grid;
    }

    private static PlayerSimulationAttributes BuildPlayerAttributes(PlayerSpawnData data)
    {
        var vocationType = (VocationType)data.Vocation;
        var vocation = Vocation.Create(vocationType, data.Level);
        var progress = Progress.Create(data.Level, 0);

        var totalStats = BuildBaseStats(vocationType, data.Level);
        var combatStats = CombatStats.BuildFrom(totalStats, vocation);
        var hp = Health.Create(totalStats, progress, data.Health);
        var mp = Mana.Create(totalStats, progress, data.Mana);

        return CreateAttributes(
            ownership: new PlayerOwnership { AccountId = data.AccountId, CharacterId = data.CharacterId },
            name: Name.Create(data.Name),
            vocation: vocation,
            progress: progress,
            stats: totalStats,
            combatStats: combatStats,
            hp: hp,
            mp: mp,
            position: new GridPosition(data.X, data.Y));
    }

    private static BaseStats BuildBaseStats(VocationType vocation, int level)
    {
        if (!VocationRegistry.TryGet(vocation, out var info) || info is null)
            return BaseStats.Zero;

        var growth = info.GrowthModifiers * Math.Max(0, level - 1);
        return info.BaseStats + growth;
    }

    private static PlayerSimulationAttributes CreateAttributes(
        PlayerOwnership ownership,
        Name name,
        Vocation vocation,
        Progress progress,
        BaseStats stats,
        CombatStats combatStats,
        Health hp,
        Mana mp,
        GridPosition position)
    {
        var type = typeof(PlayerSimulationAttributes);
        var constructor = type.GetConstructors(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)[0];
        return (PlayerSimulationAttributes)constructor.Invoke(new object[]
        {
            ownership, name, vocation, progress, Equipment.Empty, stats, combatStats, hp, mp, position
        });
    }

    public void Dispose()
    {
        Arch.Core.World.Destroy(World);
    }
}

/// <summary>
/// Dados para spawn de player no ECS.
/// </summary>
public readonly record struct PlayerSpawnData(
    int AccountId,
    int CharacterId,
    int NetworkId,
    string Name,
    int X,
    int Y,
    int Level,
    byte Vocation,
    int Health,
    int Mana);
