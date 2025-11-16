using MemoryPack;

namespace Game.Network.Packets.Game;

/// <summary>
/// Bidirectional chat packet used by both clients and the server.
/// </summary>
[MemoryPackable]
public readonly partial record struct ChatMessagePacket(
    int SenderPlayerId,
    int SenderNetworkId,
    string SenderName,
    string Message,
    long TimestampUnixMs,
    bool IsHistory,
    bool IsSystem)
{
    public static ChatMessagePacket CreateSystem(string message, long timestampUnixMs, bool isHistory = false)
        => new(0, 0, "System", message, timestampUnixMs, isHistory, true);
}
