using System;

namespace Game.Server.Chat;

public readonly record struct ChatMessage(
    int PlayerId,
    int NetworkId,
    string PlayerName,
    string Content,
    DateTimeOffset Timestamp,
    bool IsSystem)
{
    public long TimestampUnixMs => Timestamp.ToUnixTimeMilliseconds();
}
