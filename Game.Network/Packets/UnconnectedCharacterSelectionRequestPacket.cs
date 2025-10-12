using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

[MemoryPackable]
public readonly partial struct UnconnectedCharacterSelectionRequestPacket(string sessionToken, int characterId) : IPacket
{
    public string SessionToken { get; init; } = sessionToken;
    public int CharacterId { get; init; } = characterId;
}
