namespace Game.Contracts;

public readonly record struct AuthLoginRequest(string Username, string Password);
public readonly record struct AuthLoginResponse(bool Success, string? Token, string? Error);
public readonly record struct CharacterSummary(int Id, string Name);
public readonly record struct CharacterListRequest(string Token);
public readonly record struct CharacterListResponse(bool Success, string? Error, List<CharacterSummary> Characters);
public readonly record struct SelectCharacterRequest(string Token, int CharacterId);
public readonly record struct SelectCharacterResponse(bool Success, string? Error, string? WorldEndpoint, string? EnterTicket);
