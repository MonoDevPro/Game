using Game.Network.Abstractions;
using Game.Network.Packets.DTOs;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client notification when a player enters the world.
/// </summary>
[MemoryPackable]
public partial struct PlayerSpawnPacket(PlayerSnapshot player) : IPacket
{
    public PlayerSnapshot Player { get; set; } = player;
}