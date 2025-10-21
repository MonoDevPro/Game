using Arch.Core;
using Arch.System;

namespace Game.ECS.Systems;

/// <summary>
/// Base abstrata para todos os sistemas do jogo. Encapsula o acesso ao <see cref="GameEventSystem"/>.
/// </summary>
public abstract partial class GameSystem : BaseSystem<World, float>
{
	protected GameEventSystem Events { get; }

	protected GameSystem(World world, GameEventSystem events) : base(world)
	{
		Events = events;
	}
}