using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.ArchECS.Commons.Systems;

/// <summary>
/// Base abstrata para todos os sistemas do jogo. Encapsula o acesso ao <see cref="GameEventSystem"/>.
/// </summary>
public abstract partial class GameSystem(World world, ILogger? logger = null) : BaseSystem<World, long>(world)
{
    protected void LogTrace(string message, params object[] args) => logger?.LogTrace(message, args);
    protected void LogDebug(string message, params object[] args) => logger?.LogDebug(message, args);
    protected void LogInformation(string message, params object[] args) => logger?.LogInformation(message, args);
    protected void LogWarning(string message, params object[] args) => logger?.LogWarning(message, args);
    protected void LogCritical(string message, params object[] args) => logger?.LogCritical(message, args);
    protected void LogError(string message, params object[] args) => logger?.LogError(message, args);
}