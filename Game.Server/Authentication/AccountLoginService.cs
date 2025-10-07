using System;
using System.Linq;
using Game.Core.Security;
using Game.Domain.Entities;
using Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Game.Server.Authentication;

public sealed class AccountLoginService
{
    private readonly GameDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AccountLoginService> _logger;

    public AccountLoginService(GameDbContext dbContext, IPasswordHasher passwordHasher, ILogger<AccountLoginService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AccountLoginResult> AuthenticateAsync(string username, string password, string? characterName, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return AccountLoginResult.Failure("Usuário e senha são obrigatórios.");
        }

        var account = await _dbContext.Accounts
            .AsTracking()
            .Include(a => a.Characters.Where(c => c.IsActive))
            .ThenInclude(c => c.Stats)
            .Include(a => a.Characters.Where(c => c.IsActive))
            .ThenInclude(c => c.Inventory)
            .ThenInclude(i => i.Slots)
            .FirstOrDefaultAsync(a => a.Username == username, cancellationToken);

        if (account is null)
        {
            return AccountLoginResult.Failure("Conta não encontrada.");
        }

        if (!account.IsActive)
        {
            return AccountLoginResult.Failure("Conta desativada.");
        }

        if (account.IsBanned && (!account.BannedUntil.HasValue || account.BannedUntil.Value > DateTime.UtcNow))
        {
            return AccountLoginResult.Failure("Conta banida.");
        }

        if (!_passwordHasher.VerifyPassword(account.PasswordHash, password))
        {
            return AccountLoginResult.Failure("Credenciais inválidas.");
        }

        var character = ResolveCharacter(account, characterName);
        if (character is null)
        {
            return AccountLoginResult.Failure("Personagem não encontrado.");
        }

        account.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Login bem-sucedido para {Username} com personagem {Character}", account.Username, character.Name);
    return AccountLoginResult.From(account, character);
    }

    private static Character? ResolveCharacter(Account account, string? characterName)
    {
        if (account.Characters.Count == 0)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(characterName))
        {
            return account.Characters.FirstOrDefault(c => c.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase));
        }

        return account.Characters.OrderBy(c => c.Id).First();
    }
}

public sealed record AccountLoginResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Account? Account { get; init; }
    public Character? Character { get; init; }

    public static AccountLoginResult Failure(string message) => new AccountLoginResult
    {
        Success = false,
        Message = message
    };

    public static AccountLoginResult From(Account account, Character character) => new AccountLoginResult
    {
        Success = true,
        Account = account,
        Character = character
    };
}
