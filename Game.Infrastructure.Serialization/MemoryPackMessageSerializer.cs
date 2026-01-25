using Game.Contracts;

namespace Game.Infrastructure.Serialization;

public class MemoryPackMessageSerializer : IMessageSerializer
{
    public byte[] Serialize(Envelope envelope)
    {
        return EnvelopeSerializer.Serialize(envelope);
    }

    public Envelope Deserialize(ReadOnlySpan<byte> payload)
    {
        return EnvelopeSerializer.Deserialize(payload);
    }
}
