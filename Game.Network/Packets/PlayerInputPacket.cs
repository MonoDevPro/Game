using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Client -> Server player input payload (grid movement and action flags).
/// </summary>
[MemoryPackable]
public partial struct PlayerInputPacket : IPacket
{
    public uint Sequence { get; set; }
    public sbyte MoveX { get; set; }
    public sbyte MoveY { get; set; }
    public ushort Buttons { get; set; }

    public PlayerInputPacket(uint sequence, sbyte moveX, sbyte moveY, ushort buttons)
    {
        Sequence = sequence;
        MoveX = moveX;
        MoveY = moveY;
        Buttons = buttons;
    }
}
