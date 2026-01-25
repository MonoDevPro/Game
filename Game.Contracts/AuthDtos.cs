using MemoryPack;

namespace Game.Contracts;

[MemoryPackable]
public readonly partial record struct AuthLoginRequest(string Username, string Password) : IEnvelopePayload;

[MemoryPackable]
public readonly partial record struct AuthLoginResponse(bool Success, string? Token, string? Error) : IEnvelopePayload;

[MemoryPackable]
public readonly partial record struct CharacterSummary(int Id, string Name);

[MemoryPackable]
public readonly partial record struct CharacterListRequest(string Token) : IEnvelopePayload;

[MemoryPackable]
public readonly partial record struct CharacterListResponse(bool Success, string? Error, List<CharacterSummary> Characters) : IEnvelopePayload;

[MemoryPackable]
public readonly partial record struct SelectCharacterRequest(string Token, int CharacterId) : IEnvelopePayload;

[MemoryPackable]
public readonly partial record struct SelectCharacterResponse(bool Success, string? Error, string? WorldEndpoint, string? EnterTicket) : IEnvelopePayload;
