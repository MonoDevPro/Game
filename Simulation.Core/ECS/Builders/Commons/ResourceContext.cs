using Arch.Core;

namespace Simulation.Core.ECS.Builders.Commons;

public class ResourceContext(IServiceProvider rootProvider, World world)
{
    protected IServiceProvider RootServices { get; } = rootProvider;
    protected World WorldContext { get; } = world;
}
