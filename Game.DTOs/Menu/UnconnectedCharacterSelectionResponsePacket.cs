using MemoryPack;

namespace Game.Network.Packets.Menu;

[MemoryPackable]
public readonly partial record struct UnconnectedCharacterSelectionResponsePacket(
    bool Success, string Message, int CharacterId)
{
    public static UnconnectedCharacterSelectionResponsePacket Failure(string message) => new() { Success = false, Message = message, CharacterId = -1 };
    public static UnconnectedCharacterSelectionResponsePacket Ok(int characterId) => new() { Success = true, Message = string.Empty, CharacterId = characterId };
}

