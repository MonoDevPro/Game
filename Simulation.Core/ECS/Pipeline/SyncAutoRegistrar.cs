using System.Reflection;
using Arch.Core;
using Arch.System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Indexes;
using Simulation.Core.ECS.Systems;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Pipeline;

public static class SyncAutoRegistrar
{
    public static void RegisterAttributedSyncSystems<TGroup>(this TGroup group, IServiceProvider provider)
        where TGroup : Group<float>
    {
        var index = group.Get<IndexSystem>();
        
        var world = provider.GetRequiredService<World>();
        var endpoint = provider.GetRequiredService<IChannelEndpoint>();
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

        foreach (var (componentType, options) in ScanComponentsWithSyncAttribute())
        {
            var genericSystemType = typeof(Systems.GenericSyncSystem<>);
            var concreteType = genericSystemType.MakeGenericType(componentType);
            var instance = (ISystem<float>)Activator.CreateInstance(concreteType, world, endpoint, index, options)!;
            group.Add(instance);

            var logger = loggerFactory.CreateLogger("SyncAutoRegistrar");
            logger.LogInformation("[SyncAuto] Registrado GenericSyncSystem para {Component}", componentType.Name);
        }
    }

    private static IEnumerable<(Type Component, SyncOptions Options)> ScanComponentsWithSyncAttribute()
    {
        var asm = typeof(SyncAutoRegistrar).Assembly;
        foreach (var t in asm.GetTypes())
        {
            if (!t.IsValueType || t.IsPrimitive) continue;
            var attr = t.GetCustomAttribute<SyncAttribute>();
            if (attr is null) continue;
            // Só suportamos IEquatable<T> para GenericSyncSystem
            var equatable = typeof(IEquatable<>).MakeGenericType(t);
            if (!equatable.IsAssignableFrom(t)) continue;
            yield return (t, attr.ToOptions());
        }
    }
}
