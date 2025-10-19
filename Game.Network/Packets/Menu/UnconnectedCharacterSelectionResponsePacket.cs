using MemoryPack;

namespace Game.Network.Packets.Menu;

[MemoryPackable]
public readonly partial struct UnconnectedCharacterSelectionResponsePacket
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public int CharacterId { get; init; }

    public static UnconnectedCharacterSelectionResponsePacket Failure(string message) => new() { Success = false, Message = message, CharacterId = -1 };
    public static UnconnectedCharacterSelectionResponsePacket Ok(int characterId) => new() { Success = true, Message = string.Empty, CharacterId = characterId };
}

