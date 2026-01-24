using Game.Contracts;

namespace Game.Infrastructure.Serialization;

public interface IMessageSerializer
{
    byte[] Serialize(Envelope envelope);
    Envelope Deserialize(ReadOnlySpan<byte> payload);
}
