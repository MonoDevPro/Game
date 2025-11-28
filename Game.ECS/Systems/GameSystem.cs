using Arch.Core;
using Arch.System;
using Game.ECS.Components;
using Game.ECS.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Game.ECS.Systems;

/// <summary>
/// Base abstrata para todos os sistemas do jogo. Encapsula o acesso ao <see cref="GameEventSystem"/>.
/// </summary>
public abstract partial class GameSystem : BaseSystem<World, float>
{
    /// <summary>
    /// Access to game services including spatial indexing, player/npc indices, and resources.
    /// </summary>
    protected GameServices? Services { get; }
    
    /// <summary>
    /// Access to the map service for spatial queries.
    /// </summary>
    protected IMapService? MapService { get; }
    
    /// <summary>
    /// Logger instance for this system.
    /// </summary>
    protected ILogger? Logger { get; }
    
    /// <summary>
    /// Creates a GameSystem with full services and logger.
    /// </summary>
    protected GameSystem(World world, GameServices services, ILogger? logger = null) : base(world)
    {
        Services = services;
        MapService = services.MapService;
        Logger = logger;
    }
    
    /// <summary>
    /// Creates a GameSystem with just the world (for systems that don't need services).
    /// </summary>
    protected GameSystem(World world) : base(world)
    {
        Services = null;
        MapService = null;
        Logger = null;
    }
    
    /// <summary>
    /// Creates a GameSystem with world and map service.
    /// </summary>
    protected GameSystem(World world, IMapService mapService) : base(world)
    {
        Services = null;
        MapService = mapService;
        Logger = null;
    }
    
    /// <summary>
    /// Creates a GameSystem with world and logger.
    /// </summary>
    protected GameSystem(World world, ILogger? logger) : base(world)
    {
        Services = null;
        MapService = null;
        Logger = logger;
    }
    
    /// <summary>
    /// Creates a GameSystem with world, map service and logger.
    /// </summary>
    protected GameSystem(World world, IMapService mapService, ILogger? logger) : base(world)
    {
        Services = null;
        MapService = mapService;
        Logger = logger;
    }
    
    /// <summary>
    /// Updates the spatial index when an entity moves.
    /// </summary>
    protected void UpdateSpatial(Entity entity, Position oldPos, Position newPos, sbyte floor, int mapId) 
        => Services?.UpdateSpatial(entity, oldPos, newPos, floor, mapId);
    
    protected void LogTrace(string message, params object[] args) => Logger?.LogTrace(message, args);
    protected void LogDebug(string message, params object[] args) => Logger?.LogDebug(message, args);
    protected void LogInformation(string message, params object[] args) => Logger?.LogInformation(message, args);
    protected void LogWarning(string message, params object[] args) => Logger?.LogWarning(message, args);
    protected void LogCritical(string message, params object[] args) => Logger?.LogCritical(message, args);
    protected void LogError(string message, params object[] args) => Logger?.LogError(message, args);
}