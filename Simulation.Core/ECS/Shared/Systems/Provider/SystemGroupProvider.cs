using Arch.System;

namespace Simulation.Core.ECS.Shared.Systems.Provider;

public sealed class SystemGroupProvider<TFrame>(Group<TFrame> group) : ISystemGroupProvider<TFrame>
{
    public T Get<T>() where T : ISystem<TFrame>
    {
        return Group.Get<T>();
    }

    public IEnumerable<T> Find<T>() where T : ISystem<TFrame>
    {
        return Group.Find<T>();
    }

    public Group<TFrame> Group { get; } = group;
}