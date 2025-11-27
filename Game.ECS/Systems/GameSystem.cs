using Arch.Core;
using Arch.System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Game.ECS.Systems;

/// <summary>
/// Base abstrata para todos os sistemas do jogo. Encapsula o acesso ao <see cref="GameEventSystem"/>.
/// </summary>
public abstract partial class GameSystem : BaseSystem<World, float>
{
    protected GameServices Services { get; }
    protected ILogger Logger { get; }
    
    /// <summary>
    /// Base abstrata para todos os sistemas do jogo.
    /// </summary>
    protected GameSystem(World world, GameServices services, ILogger? logger = null) : base(world)
    {
        Services = services;
        Logger = logger ?? NullLogger.Instance;
    }
}