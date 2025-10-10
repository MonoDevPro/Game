using System;
using System.Linq;
using Game.Domain.Entities;
using Game.Persistence;
using Game.Server.Security;
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

    public async Task<AccountLoginResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return AccountLoginResult.Failure("Usuário e senha são obrigatórios.");
        }

        var account = await _dbContext.Accounts
            .AsTracking()
            .Include(a => a.Characters)
            .ThenInclude(c => c.Stats)
            .Include(a => a.Characters)
            .ThenInclude(s => s.Equipment)
            .Include(a => a.Characters)
            .ThenInclude(c => c.Inventory)
            .ThenInclude(i => i.Slots)
            .FirstOrDefaultAsync(a => a.Username == username, cancellationToken);

        if (account is null)
            return AccountLoginResult.Failure("Conta não encontrada.");

        if (!account.IsActive)
            return AccountLoginResult.Failure("Conta desativada.");

        if (account.IsBanned && (!account.BannedUntil.HasValue || account.BannedUntil.Value > DateTime.UtcNow))
            return AccountLoginResult.Failure("Conta banida.");

        if (!_passwordHasher.VerifyPassword(account.PasswordHash, password))
            return AccountLoginResult.Failure("Credenciais inválidas.");

        var characters = ResolveCharacters(account);
        if (characters is null)
            return AccountLoginResult.Failure("Personagem não encontrado.");

        account.LastLoginAt = DateTime.UtcNow;
        
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Login bem-sucedido para {Username}", account.Username);
    return AccountLoginResult.From(account, characters);
    }
    
    private static Character[]? ResolveCharacters(Account account)
    {
        return account.Characters.Count == 0 
            ? null 
            : account.Characters.OrderBy(c => c.Id).ToArray();
    }
}

public sealed record AccountLoginResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Account? Account { get; init; }
    public Character[] Characters { get; init; } = [];

    public static AccountLoginResult Failure(string message) => new AccountLoginResult
    {
        Success = false,
        Message = message
    };

    public static AccountLoginResult From(Account account, Character[] characters) => new AccountLoginResult
    {
        Success = true,
        Account = account,
        Characters = characters
    };
}
