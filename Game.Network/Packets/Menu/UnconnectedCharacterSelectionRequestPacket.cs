using MemoryPack;

namespace Game.Network.Packets.Menu;

[MemoryPackable]
public readonly partial record struct UnconnectedCharacterSelectionRequestPacket(
    string SessionToken,
    int CharacterId);
