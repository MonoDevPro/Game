using Arch.Core;
using Arch.System;
using Game.ECS.Entities;
using Game.ECS.Entities.Factories;

namespace Game.ECS.Systems;

/// <summary>
/// Base abstrata para todos os sistemas do jogo. Encapsula o acesso ao <see cref="GameEventSystem"/>.
/// </summary>
public abstract partial class GameSystem : BaseSystem<World, float>
{
	/// <summary>
	/// Base abstrata para todos os sistemas do jogo. Encapsula o acesso ao <see cref="GameEventSystem"/>.
	/// </summary>
	protected GameSystem(World world, GameEventSystem events, EntityFactory factory) : base(world)
	{
		Events = events;
		EntityFactory = factory;
	}

	protected GameEventSystem Events { get; }
	protected EntityFactory EntityFactory { get; }
}