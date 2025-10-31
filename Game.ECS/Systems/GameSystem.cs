using Arch.Core;
using Arch.System;

namespace Game.ECS.Systems;

/// <summary>
/// Base abstrata para todos os sistemas do jogo. Encapsula o acesso ao <see cref="GameEventSystem"/>.
/// </summary>
public abstract partial class GameSystem : BaseSystem<World, float>
{
    /// <summary>
    /// Base abstrata para todos os sistemas do jogo.
    /// </summary>
    protected GameSystem(World world) : base(world)
    {
    }
}