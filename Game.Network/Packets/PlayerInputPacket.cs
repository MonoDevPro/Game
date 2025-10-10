using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Client -> Server player input payload (grid movement and action flags).
/// </summary>
[MemoryPackable]
public readonly partial struct PlayerInputPacket(sbyte moveX, sbyte moveY, ushort buttons) : IPacket
{
    public sbyte MoveX { get; init; } = moveX;
    public sbyte MoveY { get; init; } = moveY;
    public ushort Buttons { get; init; } = buttons;
}
