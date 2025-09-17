using Simulation.Core.Network.Contracts;

namespace Simulation.Core.Options;

[AttributeUsage(AttributeTargets.Struct)]
public sealed class SyncAttribute : Attribute
{
    public Authority Authority { get; init; } = Authority.Server;
    public SyncTrigger Trigger { get; init; } = SyncTrigger.OnChange;
    public ushort SyncRateTicks { get; init; } = 0;
    public bool SyncOnSpawn { get; init; } = true;
    public bool SyncOnDespawn { get; init; } = true;
    public NetworkDeliveryMethod DeliveryMethod { get; init; } = NetworkDeliveryMethod.ReliableOrdered;
}

public static class SyncAttributeExtensions
{
    public static SyncOptions ToOptions(this SyncAttribute attr) => new()
    {
        Authority = attr.Authority,
        Trigger = attr.Trigger,
        SyncRateTicks = attr.SyncRateTicks,
        SyncOnSpawn = attr.SyncOnSpawn,
        SyncOnDespawn = attr.SyncOnDespawn,
        DeliveryMethod = attr.DeliveryMethod
    };
}