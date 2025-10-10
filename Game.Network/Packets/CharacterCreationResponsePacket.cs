using Game.Network.Abstractions;
using Game.Network.Packets.DTOs;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client notification when a player leaves the world.
/// </summary>
[MemoryPackable]
public readonly partial struct CharacterCreationResponsePacket : IPacket
{
    public bool Success { get; init; }
    public string Message { get; init; }
    public PlayerCharData CreatedCharacter { get; init; }
    
    private CharacterCreationResponsePacket(bool success, string message, PlayerCharData createdCharacter)
    {
        Success = success;
        Message = message;
        CreatedCharacter = createdCharacter;
    }
    
    public static CharacterCreationResponsePacket Failure(string message) => new(false, message, default);
    public static CharacterCreationResponsePacket Ok(PlayerCharData createdCharacter) => new(true, string.Empty, createdCharacter);
}