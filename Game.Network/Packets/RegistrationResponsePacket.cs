using Game.Abstractions.Network;
using MemoryPack;

namespace Game.Network.Packets;

/// <summary>
/// Server -> Client acknowledgement for registration attempts.
/// </summary>
[MemoryPackable]
public partial struct RegistrationResponsePacket : IPacket
{
    public bool Success { get; set; }
    public string Message { get; set; }

    public RegistrationResponsePacket(bool success, string message)
    {
        Success = success;
        Message = message;
    }

    public static RegistrationResponsePacket Failure(string message) => new(false, message);
    public static RegistrationResponsePacket Ok() => new(true, string.Empty);
}
