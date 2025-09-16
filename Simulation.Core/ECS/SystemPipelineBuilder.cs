using Arch.System;
using Microsoft.Extensions.DependencyInjection;

namespace Simulation.Core.ECS;

/// <summary>
/// Helper fluente para reduzir o boilerplate de adicionar sistemas a um Group.
/// Permite encadear Add<TSystem>() e garantir a ordem explícita num único lugar.
/// </summary>
public sealed class SystemPipelineBuilder<TData> where TData : notnull
{
    private readonly Group<TData> _group;
    private readonly IServiceProvider _provider;

    public SystemPipelineBuilder(Group<TData> group, IServiceProvider provider)
    {
        _group = group;
        _provider = provider;
    }

    public SystemPipelineBuilder<TData> Add<TSystem>() where TSystem : class, ISystem<TData>
    {
        _group.Add(_provider.GetRequiredService<TSystem>());
        return this;
    }

    public Group<TData> Build() => _group;
}
