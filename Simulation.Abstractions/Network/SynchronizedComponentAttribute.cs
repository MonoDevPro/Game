using System;

namespace Simulation.Abstractions.Network;

public enum Authority { Server, Client }
public enum SyncTrigger { OnChange, OnTick }

[AttributeUsage(AttributeTargets.Struct)]
public class SynchronizedComponentAttribute(
    Authority authority,
    SyncTrigger trigger = SyncTrigger.OnChange,
    ushort syncRateTicks = 0)
    : Attribute
{
    public Authority Authority { get; } = authority;
    public SyncTrigger Trigger { get; } = trigger;
    public ushort SyncRateTicks { get; } = syncRateTicks;
}
