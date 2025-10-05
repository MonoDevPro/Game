using Arch.Core;
using Arch.System;
using Game.Abstractions.Network;

namespace Game.ECS.Systems;

public abstract class GameSystem(World world) : BaseSystem<World, float>(world);