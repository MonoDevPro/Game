using Game.Network.Abstractions;
using MemoryPack;

namespace Game.Network.Packets;

[MemoryPackable]
public readonly partial struct CharacterSelectionResponsePacket : IPacket
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public int CharacterId { get; init; }

    public static CharacterSelectionResponsePacket Failure(string message) => new() { Success = false, Message = message, CharacterId = -1 };
    public static CharacterSelectionResponsePacket Ok(int characterId) => new() { Success = true, Message = string.Empty, CharacterId = characterId };
}

