using Game.Abstractions.Network;
using Game.Domain.Enums;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client delta update for a player's position and facing.
/// </summary>
[MemoryPackable]
public partial struct PlayerStatePacket : IPacket
{
    public int NetworkId { get; set; }
    public GridPosition Position { get; set; }
    public DirectionEnum Facing { get; set; }
    public uint Tick { get; set; }

    public PlayerStatePacket(int networkId, GridPosition position, DirectionEnum facing, uint tick)
    {
        NetworkId = networkId;
        Position = position;
        Facing = facing;
        Tick = tick;
    }
}
