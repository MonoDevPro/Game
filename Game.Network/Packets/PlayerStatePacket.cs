using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client delta update for a player's position and facing.
/// </summary>
[MemoryPackable]
public partial struct PlayerStatePacket(int networkId, Coordinate position, Coordinate facing)
    : IPacket
{
    public int NetworkId { get; set; } = networkId;
    public Coordinate Position { get; set; } = position;
    public Coordinate Facing { get; set; } = facing;
}