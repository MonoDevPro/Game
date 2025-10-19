using MemoryPack;

namespace Game.Network.Packets.Menu;

[MemoryPackable]
public readonly partial record struct UnconnectedCharacterDeleteRequestPacket(
    string SessionToken,
    int CharacterId);
