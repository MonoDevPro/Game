using MemoryPack;

namespace Game.Network.Packets.Game;

[MemoryPackable]
public readonly partial record struct PlayerJoinPacket(
    MapDataPacket MapDataPacket,
    PlayerDataPacket LocalPlayer,
    PlayerDataPacket[] OtherPlayers);