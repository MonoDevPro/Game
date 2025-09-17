using Simulation.Core.Network.Contracts;

namespace Simulation.Core.Options;

public enum Authority { Server, Client }
public enum SyncTrigger { OnChange, OnTick, OnSpawn }

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
