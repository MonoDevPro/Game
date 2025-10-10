using System.Security.Cryptography;
using Game.Domain.Entities;
using Game.Persistence;
using Game.Server.Security;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Authentication;

public sealed class AccountRegistrationService(
    GameDbContext dbContext,
    IPasswordHasher passwordHasher,
    ILogger<AccountRegistrationService> logger)
{
    public async Task<AccountRegistrationResult> RegisterAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken)
    {
        username = username.Trim();
        email = email.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
        {
            return AccountRegistrationResult.Failure("Usuário, senha e e-mail são obrigatórios.");
        }

        if (await dbContext.Accounts.AnyAsync(a => a.Username == username, cancellationToken))
        {
            return AccountRegistrationResult.Failure("Nome de usuário já está em uso.");
        }

        if (await dbContext.Accounts.AnyAsync(a => a.Email == email, cancellationToken))
        {
            return AccountRegistrationResult.Failure("E-mail já está cadastrado.");
        }

        try
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            var passwordHash = passwordHasher.HashPassword(password);

            var account = new Account
            {
                Username = username,
                Email = email,
                PasswordHash = passwordHash,
                PasswordSalt = salt,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow,
                IsActive = true,
                IsEmailVerified = false,
                Characters = new List<Character>()
            };

            await dbContext.Accounts.AddAsync(account, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);

            logger.LogInformation("New account registered: {Username}", username);

            return AccountRegistrationResult.Succeeded(account);
        }
        catch (DbUpdateException dbEx)
        {
            logger.LogError(dbEx, "Database failure while registering account {Username}", username);
            return AccountRegistrationResult.Failure("Erro ao salvar dados no banco.");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while registering account {Username}", username);
            return AccountRegistrationResult.Failure("Erro interno ao registrar conta.");
        }
    }
}

public sealed record AccountRegistrationResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public Account? Account { get; init; }

    public static AccountRegistrationResult Failure(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };

    public static AccountRegistrationResult Succeeded(Account account) => new()
    {
        IsSuccess = true,
        Account = account,
    };
}
