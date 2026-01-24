namespace Game.Contracts;

public readonly record struct Envelope(OpCode OpCode, object? Payload);
