namespace Game.Contracts;

public interface IEnvelopePayload { }

public readonly record struct Envelope(OpCode OpCode, IEnvelopePayload? Payload);
