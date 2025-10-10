using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client notification when a player leaves the world.
/// </summary>
[MemoryPackable]
public partial struct PlayerDespawnPacket(int networkId) : IPacket
{
    public int NetworkId { get; set; } = networkId;
}
