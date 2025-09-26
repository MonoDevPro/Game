using Simulation.Core.Options;

namespace Simulation.Core.ECS.Sync;

/// <summary>
/// Pre-configured sync options for common use cases.
/// Provides standardized configurations to ensure consistency across the system.
/// </summary>
public static class SyncPresets
{
    /// <summary>
    /// Standard server-to-client sync for game state components.
    /// Uses reliable delivery and change-based synchronization.
    /// </summary>
    public static SyncOptions ServerStateSync { get; } = SyncOptionsBuilder
        .ServerBroadcast()
        .Build();

    /// <summary>
    /// High-frequency position synchronization with unreliable delivery.
    /// Optimized for smooth movement with acceptable packet loss.
    /// </summary>
    public static SyncOptions PositionSync { get; } = SyncOptionsBuilder
        .PositionSync()
        .Build();

    /// <summary>
    /// Client input synchronization to server.
    /// Uses sequenced delivery to ensure input ordering.
    /// </summary>
    public static SyncOptions ClientInputSync { get; } = SyncOptionsBuilder
        .ClientInput()
        .Build();

    /// <summary>
    /// Critical state sync that requires guaranteed delivery.
    /// Used for important events like death, level changes, etc.
    /// </summary>
    public static SyncOptions CriticalStateSync { get; } = SyncOptionsBuilder
        .ServerBroadcast()
        .Build();

    /// <summary>
    /// One-shot event synchronization for temporary notifications.
    /// Automatically removes the component after sending.
    /// </summary>
    public static SyncOptions EventSync { get; } = SyncOptionsBuilder
        .ServerBroadcast()
        .WithFrequency(SyncFrequency.OneShot)
        .Build();
}