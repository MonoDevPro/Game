using Game.Domain;

namespace Game.Application;

public interface IAccountRepository
{
    Task<Account?> FindByUsernameAsync(string username, CancellationToken ct = default);
}

public interface ICharacterRepository
{
    Task<IReadOnlyList<Character>> ListByAccountIdAsync(int accountId, CancellationToken ct = default);
    Task<Character?> FindByIdAsync(int id, CancellationToken ct = default);
    Task UpdateAsync(Character character, CancellationToken ct = default);
}

public interface ICharacterVocationRepository
{
    Task<IReadOnlyList<CharacterVocation>> ListByAccountIdAsync(int accountId, CancellationToken ct = default);
    Task<CharacterVocation?> FindByCharacterIdAsync(int characterId, CancellationToken ct = default);
    Task<CharacterVocation?> FindByIdAsync(int id, CancellationToken ct = default);
    Task UpdateAsync(CharacterVocation vocation, CancellationToken ct = default);
}


public interface ISessionService
{
    string CreateSession(int accountId);
    bool TryGetAccountId(string token, out int accountId);
}

public interface IEnterTicketService
{
    string IssueTicket(int characterId);
    bool TryConsumeTicket(string ticket, out int characterId);
}
