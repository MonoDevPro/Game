using Arch.System;
using Microsoft.Extensions.DependencyInjection;

namespace Simulation.Core.ECS.Shared.Systems.Provider;

public static class GroupExtensions
{
    // Registra o Group<TFrame> e o provider wrapper.
    public static IServiceCollection AddSystemGroup<TFrame>(this IServiceCollection services, Group<TFrame> group)
    {
        services.AddSingleton(group);
        services.AddSingleton<ISystemGroupProvider<TFrame>>(sp => new SystemGroupProvider<TFrame>(group));
        return services;
    }
    
    // Conveniência: registra e adiciona sistema ao group (instância obtida do provider).
    public static IServiceCollection AddAndAddToGroup<TSys, TFrame>(this IServiceCollection services)
        where TSys : class, ISystem<TFrame>
    {
        services.AddSingleton<TSys>();
        return services;
    }
}