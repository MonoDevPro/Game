using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client notification when a player leaves the world.
/// </summary>
[MemoryPackable]
public partial struct PlayerDespawnPacket : IPacket
{
    public int NetworkId { get; set; }

    public PlayerDespawnPacket(int networkId)
    {
        NetworkId = networkId;
    }
}
