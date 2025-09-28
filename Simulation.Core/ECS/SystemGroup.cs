using Arch.Core;
using Arch.System;

namespace Simulation.Core.ECS;

public class SystemGroup(string name, World world) : Group<float>(name)
{
    public World WorldContext { get; } = world;
}