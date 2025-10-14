using Game.Domain.Entities;
using Game.Persistence.Interfaces;
using Game.Server.Security;

namespace Game.Server.Authentication;

public sealed record AccountLoginResult(bool Success, string Message, Account? Account)
{
    public static AccountLoginResult Failure(string message) => new(false, message, null);
    public static AccountLoginResult From(Account account, Character[] characters) => new(true, "Login successful", account);
}

/// <summary>
/// Serviço responsável pela autenticação de contas.
/// Utiliza AccountCharacterService para gerenciar personagens.
/// 
/// Autor: MonoDevPro
/// Data: 2025-10-12 21:31:29
/// </summary>
public sealed class AccountLoginService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ILogger<AccountLoginService> logger)
{

    /// <summary>
    /// Autentica um usuário com username e senha.
    /// </summary>
    public async Task<AccountLoginResult> AuthenticateAsync(
        string username, 
        string password, 
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return AccountLoginResult.Failure("Usuário e senha são obrigatórios.");

        // ✅ Usar repositório especializado
        var account = await unitOfWork.Accounts.GetByUsernameWithCharactersAsync(username, cancellationToken);

        if (account is null)
        {
            logger.LogWarning("Login attempt failed: Account not found for username {Username}", username);
            return AccountLoginResult.Failure("Credenciais inválidas.");
        }

        var validationResult = ValidateAccountState(account);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Login attempt failed for {Username}: {Reason}", username, validationResult.ErrorMessage);
            return AccountLoginResult.Failure(validationResult.ErrorMessage!);
        }

        if (!passwordHasher.VerifyPassword(account.PasswordHash, password))
        {
            logger.LogWarning("Login attempt failed: Invalid password for username {Username}", username);
            return AccountLoginResult.Failure("Credenciais inválidas.");
        }

        // Atualizar último login
        account.LastLoginAt = DateTime.UtcNow;
        await unitOfWork.Accounts.UpdateAsync(account, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Login successful for {Username} (Account ID: {AccountId})",
            account.Username, account.Id);

        return AccountLoginResult.From(account, account.Characters.ToArray());
    }

    /// <summary>
    /// Valida se a conta está em um estado válido para login.
    /// </summary>
    private static (bool IsValid, string? ErrorMessage) ValidateAccountState(Account account)
    {
        if (!account.IsActive)
        {
            return (false, "Conta desativada.");
        }

        if (account.IsBanned)
        {
            // Verificar se o ban é permanente ou temporário
            if (!account.BannedUntil.HasValue)
            {
                return (false, "Conta banida permanentemente.");
            }

            if (account.BannedUntil.Value > DateTime.UtcNow)
            {
                var timeRemaining = account.BannedUntil.Value - DateTime.UtcNow;
                var message = timeRemaining.TotalHours > 24
                    ? $"Conta banida até {account.BannedUntil.Value:dd/MM/yyyy HH:mm} UTC."
                    : $"Conta banida por mais {Math.Ceiling(timeRemaining.TotalHours)} horas.";
                
                return (false, message);
            }

            // Ban expirou - poderia remover o flag aqui se desejar
            // account.IsBanned = false;
            // account.BannedUntil = null;
        }

        return (true, null);
    }
}