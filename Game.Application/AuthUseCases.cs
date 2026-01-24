using Game.Contracts;
using Game.Domain;

namespace Game.Application;

public sealed class AuthUseCases
{
    private readonly IAccountRepository _accounts;
    private readonly ICharacterRepository _characters;
    private readonly ISessionService _sessions;
    private readonly IEnterTicketService _tickets;

    public AuthUseCases(
        IAccountRepository accounts,
        ICharacterRepository characters,
        ISessionService sessions,
        IEnterTicketService tickets)
    {
        _accounts = accounts;
        _characters = characters;
        _sessions = sessions;
        _tickets = tickets;
    }

    public async Task<Result<AuthLoginResponse>> LoginAsync(AuthLoginRequest request, CancellationToken ct = default)
    {
        var account = await _accounts.FindByUsernameAsync(request.Username, ct);
        if (account is null)
        {
            return Result<AuthLoginResponse>.Fail("Invalid credentials");
        }

        if (!PasswordHasher.Verify(request.Password, account.PasswordHash))
        {
            return Result<AuthLoginResponse>.Fail("Invalid credentials");
        }

        var token = _sessions.CreateSession(account.Id);
        return Result<AuthLoginResponse>.Ok(new AuthLoginResponse(true, token, null));
    }

    public async Task<Result<CharacterListResponse>> ListCharactersAsync(CharacterListRequest request, CancellationToken ct = default)
    {
        if (!_sessions.TryGetAccountId(request.Token, out var accountId))
        {
            return Result<CharacterListResponse>.Fail("Invalid session");
        }

        var characters = await _characters.ListByAccountIdAsync(accountId, ct);
        var list = characters.Select(c => new CharacterSummary(c.Id, c.Name)).ToList();
        return Result<CharacterListResponse>.Ok(new CharacterListResponse(true, null, list));
    }

    public async Task<Result<SelectCharacterResponse>> SelectCharacterAsync(SelectCharacterRequest request, string worldEndpoint, CancellationToken ct = default)
    {
        if (!_sessions.TryGetAccountId(request.Token, out var accountId))
        {
            return Result<SelectCharacterResponse>.Fail("Invalid session");
        }

        var character = await _characters.FindByIdAsync(request.CharacterId, ct);
        if (character is null || character.AccountId != accountId)
        {
            return Result<SelectCharacterResponse>.Fail("Character not found");
        }

        var ticket = _tickets.IssueTicket(character.Id);
        return Result<SelectCharacterResponse>.Ok(new SelectCharacterResponse(true, null, worldEndpoint, ticket));
    }
}
