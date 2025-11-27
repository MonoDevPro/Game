using Arch.Core;
using Arch.System;
using Game.ECS.Components;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Game.ECS.Systems;

/// <summary>
/// Base abstrata para todos os sistemas do jogo. Encapsula o acesso ao <see cref="GameEventSystem"/>.
/// </summary>
public abstract partial class GameSystem(World world, GameServices services, ILogger? logger = null)
    : BaseSystem<World, float>(world)
{
    private GameServices Services { get; } = services;
    private ILogger? Logger { get; } = logger;
    
    protected void UpdateSpatial(Entity entity, Position oldPos, Position newPos, sbyte floor, int mapId) 
        => Services.UpdateSpatial(entity, oldPos, newPos, floor, mapId);
    
    protected void LogTrace(string message, params object[] args) => Logger?.LogTrace(message, args);
    protected void LogDebug(string message, params object[] args) => Logger?.LogDebug(message, args);
    protected void LogInformation(string message, params object[] args) => Logger?.LogInformation(message, args);
    protected void LogWarning(string message, params object[] args) => Logger?.LogWarning(message, args);
    protected void LogCritical(string message, params object[] args) => Logger?.LogCritical(message, args);
    protected void LogError(string message, params object[] args) => Logger?.LogError(message, args);
}