using Simulation.Core.Options;
using Simulation.Core.Ports.Network;

namespace Simulation.Core.ECS.Sync;

/// <summary>
/// Builder pattern for creating SyncOptions with fluent API and sensible defaults.
/// Reduces boilerplate and provides commonly used configurations.
/// </summary>
public class SyncOptionsBuilder
{
    private Authority _authority = Authority.Server;
    private SyncFrequency _frequency = SyncFrequency.OnChange;
    private SyncTarget _target = SyncTarget.Broadcast;
    private NetworkDeliveryMethod _deliveryMethod = NetworkDeliveryMethod.ReliableOrdered;
    private ushort _syncRateTicks = 0;

    private SyncOptionsBuilder() { }

    /// <summary>
    /// Creates a new SyncOptionsBuilder with default values.
    /// </summary>
    public static SyncOptionsBuilder Create() => new();

    /// <summary>
    /// Creates a SyncOptionsBuilder with server broadcast defaults.
    /// Most common configuration for server-authoritative components.
    /// </summary>
    public static SyncOptionsBuilder ServerBroadcast() => new()
    {
        _authority = Authority.Server,
        _frequency = SyncFrequency.OnChange,
        _target = SyncTarget.Broadcast,
        _deliveryMethod = NetworkDeliveryMethod.ReliableOrdered,
        _syncRateTicks = 0
    };

    /// <summary>
    /// Creates a SyncOptionsBuilder for client-to-server input synchronization.
    /// </summary>
    public static SyncOptionsBuilder ClientInput() => new()
    {
        _authority = Authority.Client,
        _frequency = SyncFrequency.OnChange,
        _target = SyncTarget.Unicast,
        _deliveryMethod = NetworkDeliveryMethod.Sequenced,
        _syncRateTicks = 0
    };

    /// <summary>
    /// Creates a SyncOptionsBuilder for high-frequency position updates.
    /// </summary>
    public static SyncOptionsBuilder PositionSync() => new()
    {
        _authority = Authority.Server,
        _frequency = SyncFrequency.OnTick,
        _target = SyncTarget.Broadcast,
        _deliveryMethod = NetworkDeliveryMethod.Sequenced,
        _syncRateTicks = 3 // ~20Hz at 60FPS
    };

    public SyncOptionsBuilder WithAuthority(Authority authority)
    {
        _authority = authority;
        return this;
    }

    public SyncOptionsBuilder WithFrequency(SyncFrequency frequency)
    {
        _frequency = frequency;
        return this;
    }

    public SyncOptionsBuilder WithTarget(SyncTarget target)
    {
        _target = target;
        return this;
    }

    public SyncOptionsBuilder WithDeliveryMethod(NetworkDeliveryMethod deliveryMethod)
    {
        _deliveryMethod = deliveryMethod;
        return this;
    }

    public SyncOptionsBuilder WithSyncRate(ushort ticks)
    {
        _syncRateTicks = ticks;
        return this;
    }

    /// <summary>
    /// Builds the final SyncOptions instance.
    /// </summary>
    public SyncOptions Build()
    {
        return new SyncOptions(_authority, _frequency, _target, _deliveryMethod, _syncRateTicks);
    }
}