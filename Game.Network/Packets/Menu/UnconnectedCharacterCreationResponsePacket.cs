using Game.Network.Packets.DTOs;
using MemoryPack;

namespace Game.Network.Packets.Menu;

/// <summary>
/// Server -> Client notification when a player leaves the world.
/// </summary>
[MemoryPackable]
public readonly partial struct UnconnectedCharacterCreationResponsePacket
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public CharMenuData CreatedCharacter { get; init; }
    
    private UnconnectedCharacterCreationResponsePacket(bool success, string message, CharMenuData createdCharacter)
    {
        Success = success;
        Message = message;
        CreatedCharacter = createdCharacter;
    }
    
    public static UnconnectedCharacterCreationResponsePacket Failure(string message) => new(false, message, default);
    public static UnconnectedCharacterCreationResponsePacket Ok(CharMenuData createdCharacter) => new(true, string.Empty, createdCharacter);
}