using Arch.System;

namespace Simulation.Core.ECS.Shared.Systems.Provider;

public interface ISystemGroupProvider<TFrame>
{
    T Get<T>() where T : ISystem<TFrame>;
    IEnumerable<T> Find<T>() where T : ISystem<TFrame>;
    Group<TFrame> Group { get; }
}