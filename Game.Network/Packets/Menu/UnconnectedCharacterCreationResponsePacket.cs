using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Server -> Client notification when a player leaves the world.
/// </summary>
[MemoryPackable]
public readonly partial record struct UnconnectedCharacterCreationResponsePacket(
    bool Success,
    string Message,
    CharMenuData CreatedCharacter)
{
    public static UnconnectedCharacterCreationResponsePacket Failure(string message) => new(false, message, default);
    public static UnconnectedCharacterCreationResponsePacket Ok(CharMenuData createdCharacter) => new(true, string.Empty, createdCharacter);
}