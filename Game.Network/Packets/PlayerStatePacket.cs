using Game.Abstractions.Network;
using Game.Domain.Enums;
using Game.Domain.VOs;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client delta update for a player's position and facing.
/// </summary>
[MemoryPackable]
public partial struct PlayerStatePacket(int networkId, Coordinate position, DirectionEnum facing, uint tick)
    : IPacket
{
    public int NetworkId { get; set; } = networkId;
    public Coordinate Position { get; set; } = position;
    public DirectionEnum Facing { get; set; } = facing;
    public uint Tick { get; set; } = tick;
}
