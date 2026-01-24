using Game.Contracts;

namespace Game.Infrastructure.Serialization;

public class MemoryPackMessageSerializer : IMessageSerializer
{
    public byte[] Serialize(Envelope envelope)
    {
        return MemoryPack.MemoryPackSerializer.Serialize(envelope);
    }

    public Envelope Deserialize(ReadOnlySpan<byte> payload)
    {
        return MemoryPack.MemoryPackSerializer.Deserialize<Envelope>(payload)!;
    }
}