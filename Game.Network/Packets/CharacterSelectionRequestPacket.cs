using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

[MemoryPackable]
public readonly partial struct CharacterSelectionRequestPacket(int characterId) : IPacket
{
    public int CharacterId { get; init; } = characterId;
}
