using Game.Domain.Enums;
using Game.Network.Abstractions;
using Game.Network.Packets.DTOs;
using MemoryPack;

namespace Game.Network.Packets;

[MemoryPackable]
public readonly partial struct GameDataPacket : IPacket
{
    public MapData MapData { get; init; }
    public PlayerSnapshot LocalPlayer { get; init; }
    public PlayerSnapshot[] OtherPlayers { get; init; }
}