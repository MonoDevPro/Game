using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client notification when a player enters the world.
/// </summary>
[MemoryPackable]
public partial struct PlayerSpawnPacket : IPacket
{
    public PlayerSnapshot Player { get; set; }

    public PlayerSpawnPacket(PlayerSnapshot player)
    {
        Player = player;
    }
}
