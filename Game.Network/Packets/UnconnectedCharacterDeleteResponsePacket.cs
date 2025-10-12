using Game.Domain.Enums;
using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

[MemoryPackable]
public readonly partial record struct UnconnectedCharacterDeleteResponsePacket(
    bool Success,
    string Message,
    int CharacterId) : IPacket
{
    public static UnconnectedCharacterDeleteResponsePacket Failure(string message) => new(false, message, -1);
    public static UnconnectedCharacterDeleteResponsePacket Ok(int characterId) => new(true, string.Empty, characterId);
}


