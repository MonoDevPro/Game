using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.LowLevel;
using Arch.System;
using Game.Domain.Templates;
using Game.ECS.Components;
using Game.ECS.Entities.Npc;
using Game.ECS.Entities.Player;
using Game.ECS.Services;
using Game.ECS.Systems;
using Microsoft.Extensions.Logging;

namespace Game.ECS;

/// <summary>
/// Implementa um timestep fixo para simulação determinística.
/// Acumula delta times e executa updates em intervalos fixos.
/// </summary>
public class FixedTimeStep(float fixedDeltaTime)
{
    private float _accumulator;

    /// <summary>
    /// Acumula tempo delta. Limita a 0.25s para evitar "spiral of death".
    /// </summary>
    public void Accumulate(float deltaTime)
    {
        _accumulator += Math.Min(deltaTime, 0.25f);
    }

    /// <summary>
    /// Verifica se um update deve ser executado.
    /// </summary>
    public bool ShouldUpdate()
    {
        return _accumulator >= fixedDeltaTime;
    }

    /// <summary>
    /// Consome um timestep do acumulador.
    /// </summary>
    public void Step()
    {
        _accumulator -= fixedDeltaTime;
    }
}

/// <summary>
/// Base abstrata para a simulação do jogo usando ECS.
/// Gerencia o World (mundo de entidades), systems (sistemas) e o loop de simulação com timestep fixo.
/// Pode ser usado tanto como server (full simulation) quanto client (partial simulation).
/// </summary>
public abstract class GameSimulation : GameSystem
{
    protected readonly Group<float> Systems;
    private readonly FixedTimeStep _fixedTimeStep;
    
    public uint CurrentTick { get; private set; }
    protected ILoggerFactory LoggerFactory { get; }
    
    /// <summary>
    /// Access to the player index for lookups by network ID.
    /// </summary>
    public IPlayerIndex PlayerIndex => Services!.PlayerIndex;
    
    /// <summary>
    /// Access to the NPC index for lookups by network ID.
    /// </summary>
    public INpcIndex NpcIndex => Services!.NpcIndex;
    
    /// <summary>
    /// Access to the map service for spatial queries.
    /// </summary>
    public new IMapService? MapService => Services!.MapService;
    
    /// <summary>
    /// Registers a map with the map service.
    /// </summary>
    public void RegisterMap(int mapId, IMapGrid mapGrid, IMapSpatial mapSpatial)
    {
        Services!.MapService?.RegisterMap(mapId, mapGrid, mapSpatial);
    }
    
    /// <summary>
    /// Tries to get any entity (player or NPC) by network ID.
    /// </summary>
    public bool TryGetAnyEntity(int networkId, out Entity entity)
    {
        return Services!.TryGetAnyEntity(networkId, out entity);
    }

    protected GameSimulation(GameServices services, ILoggerFactory loggerFactory) 
        : base(
            World.Create(
                chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
                minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
                archetypeCapacity: SimulationConfig.ArchetypeCapacity,
                entityCapacity: SimulationConfig.EntityCapacity), 
            services, 
            loggerFactory.CreateLogger<GameSimulation>())
    {
        Systems = new Group<float>(SimulationConfig.SimulationName);
        _fixedTimeStep = new FixedTimeStep(SimulationConfig.TickDelta);
        LoggerFactory = loggerFactory;
    }

    /// <summary>
    /// Configuração de sistemas. Deve ser implementada por subclasses para adicionar sistemas específicos.
    /// </summary>
    protected abstract void ConfigureSystems(World world, GameServices services, Group<float> systems);
    
    /// <summary>
    /// Helper for creating string handles using GameResources.
    /// </summary>
    protected Handle<string> CreateStringHandle(string value) => Services!.Resources.Strings.Register(value);
    
    /// <summary>
    /// Helper for releasing string handles.
    /// </summary>
    protected void ReleaseStringHandle(Handle<string> handle) => Services!.Resources.Strings.Unregister(handle);

    #region Player Lifecycle
    
    /// <summary>
    /// Creates a player entity from a template.
    /// </summary>
    public Entity CreatePlayer(PlayerTemplate template)
    {
        var entity = PlayerLifecycle.CreatePlayer(World, CreateStringHandle, template);
        PlayerIndex.Register(template.NetworkId, entity);
        Services!.RegisterEntity(entity, new Position(template.PosX, template.PosY), template.Floor, template.MapId);
        return entity;
    }
    
    /// <summary>
    /// Creates a player entity from a snapshot.
    /// </summary>
    public Entity CreatePlayer(in PlayerSnapshot snapshot)
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
            HpRegen: 0f,
            Mp: snapshot.Mp,
            MaxMp: snapshot.MaxMp,
            MpRegen: 0f,
            MovementSpeed: snapshot.MovementSpeed,
            AttackSpeed: snapshot.AttackSpeed,
            PhysicalAttack: snapshot.PhysicalAttack,
            MagicAttack: snapshot.MagicAttack,
            PhysicalDefense: snapshot.PhysicalDefense,
            MagicDefense: snapshot.MagicDefense
        );
        return CreatePlayer(template);
    }
    
    /// <summary>
    /// Destroys a player entity by network ID.
    /// </summary>
    public virtual bool DestroyPlayer(int networkId)
    {
        if (!PlayerIndex.TryGetEntity(networkId, out var entity))
            return false;
            
        PlayerLifecycle.DestroyPlayer(World, entity, ReleaseStringHandle);
        PlayerIndex.Unregister(networkId);
        return true;
    }
    
    /// <summary>
    /// Tries to get a player entity by network ID.
    /// </summary>
    public bool TryGetPlayerEntity(int networkId, out Entity entity) => PlayerIndex.TryGetEntity(networkId, out entity);
    
    #endregion
    
    #region NPC Lifecycle
    
    /// <summary>
    /// Creates an NPC entity from a template.
    /// </summary>
    public Entity CreateNpcFromTemplate(NpcTemplate template, Position position, int floor, int mapId, int networkId)
    {
        var entity = NpcLifecycle.CreateNPC(World, CreateStringHandle, template, position, floor, mapId, networkId);
        NpcIndex.Register(networkId, entity);
        Services!.RegisterEntity(entity, position, (sbyte)floor, mapId);
        return entity;
    }
    
    /// <summary>
    /// Destroys an NPC entity by network ID.
    /// </summary>
    public virtual bool DestroyNpc(int networkId)
    {
        if (!NpcIndex.TryGetEntity(networkId, out var entity))
            return false;
            
        NpcLifecycle.DestroyNPC(World, ReleaseStringHandle, entity);
        NpcIndex.Unregister(networkId);
        return true;
    }
    
    /// <summary>
    /// Tries to get an NPC entity by network ID.
    /// </summary>
    public bool TryGetNpcEntity(int networkId, out Entity entity) => NpcIndex.TryGetEntity(networkId, out entity);
    
    #endregion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override void Update(in float deltaTime)
    {
        _fixedTimeStep.Accumulate(deltaTime);
    
        while (_fixedTimeStep.ShouldUpdate())
        {
            CurrentTick++;

            Systems.BeforeUpdate(SimulationConfig.TickDelta);

            Systems.Update(SimulationConfig.TickDelta);

            Systems.AfterUpdate(SimulationConfig.TickDelta);

            _fixedTimeStep.Step();
        }
    }

    public override void Dispose()
    {
        Systems.Dispose();
        LoggerFactory.Dispose();
        base.Dispose();
    }
}