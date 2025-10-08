using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Client -> Server player input payload (grid movement and action flags).
/// </summary>
[MemoryPackable]
public partial struct PlayerInputPacket(uint sequence, sbyte moveX, sbyte moveY, ushort buttons)
    : IPacket
{
    public uint Sequence { get; set; } = sequence;
    public sbyte MoveX { get; set; } = moveX;
    public sbyte MoveY { get; set; } = moveY;
    public ushort Buttons { get; set; } = buttons;
}
