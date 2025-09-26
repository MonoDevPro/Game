using Arch.System;
using Simulation.Core.ECS.Sync;
using Simulation.Core.Options;

namespace Simulation.Core.ECS.Pipeline;

/// <summary>
/// Extension methods for SystemRegistry to provide more fluent and readable system registration.
/// </summary>
public static class SystemRegistryExtensions
{
    /// <summary>
    /// Registers multiple sync systems using the same configuration.
    /// Reduces repetitive code when multiple components share sync settings.
    /// </summary>
    public static void RegisterSyncSystems<T1, T2>(this SystemRegistry registry, SyncOptions options)
        where T1 : struct, IEquatable<T1>
        where T2 : struct, IEquatable<T2>
    {
        registry.RegisterOutboxSync<T1>(options);
        registry.RegisterOutboxSync<T2>(options);
    }

    /// <summary>
    /// Registers multiple sync systems using the same configuration.
    /// </summary>
    public static void RegisterSyncSystems<T1, T2, T3>(this SystemRegistry registry, SyncOptions options)
        where T1 : struct, IEquatable<T1>
        where T2 : struct, IEquatable<T2>
        where T3 : struct, IEquatable<T3>
    {
        registry.RegisterOutboxSync<T1>(options);
        registry.RegisterOutboxSync<T2>(options);
        registry.RegisterOutboxSync<T3>(options);
    }

    /// <summary>
    /// Registers multiple sync systems using the same configuration.
    /// </summary>
    public static void RegisterSyncSystems<T1, T2, T3, T4>(this SystemRegistry registry, SyncOptions options)
        where T1 : struct, IEquatable<T1>
        where T2 : struct, IEquatable<T2>
        where T3 : struct, IEquatable<T3>
        where T4 : struct, IEquatable<T4>
    {
        registry.RegisterOutboxSync<T1>(options);
        registry.RegisterOutboxSync<T2>(options);
        registry.RegisterOutboxSync<T3>(options);
        registry.RegisterOutboxSync<T4>(options);
    }
}