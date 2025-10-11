using Arch.Core;
using Arch.System;

namespace Game.ECS.Systems.Common;

public abstract partial class GameSystem(World world) : BaseSystem<World, float>(world);
