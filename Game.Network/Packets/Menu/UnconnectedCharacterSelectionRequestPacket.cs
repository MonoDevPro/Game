using MemoryPack;

namespace Game.Network.Packets.Menu;

[MemoryPackable]
public readonly partial struct UnconnectedCharacterSelectionRequestPacket(string sessionToken, int characterId)
{
    public string SessionToken { get; init; } = sessionToken;
    public int CharacterId { get; init; } = characterId;
}
