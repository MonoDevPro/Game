using MemoryPack;

namespace Game.Contracts;

[MemoryPackable]
public readonly partial record struct ChatSendRequest(string Channel, string Sender, string Message) : IEnvelopePayload;

[MemoryPackable]
public readonly partial record struct ChatMessage(string Channel, string Sender, string Message) : IEnvelopePayload;
