using Arch.Core;
using Arch.System;

namespace Game.ECS.Systems;

public abstract partial class GameSystem(World world) : BaseSystem<World, float>(world);