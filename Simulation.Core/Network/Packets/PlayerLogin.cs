using MemoryPack;

namespace Simulation.Core.Network.Packets;

[MemoryPackable]
public partial record struct PlayerLogin
{
    public int PlayerId { get; set; }
}