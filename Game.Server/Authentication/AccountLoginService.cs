using Game.Domain.Entities;
using Game.Persistence;
using Game.Server.Security;
using Microsoft.EntityFrameworkCore;

namespace Game.Server.Authentication;

/// <summary>
/// Serviço responsável pela autenticação de contas.
/// Utiliza AccountCharacterService para gerenciar personagens.
/// 
/// Autor: MonoDevPro
/// Data: 2025-10-12 21:31:29
/// </summary>
public sealed class AccountLoginService
{
    private readonly GameDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly AccountCharacterService _characterService;
    private readonly ILogger<AccountLoginService> _logger;

    public AccountLoginService(
        GameDbContext dbContext, 
        IPasswordHasher passwordHasher,
        AccountCharacterService characterService,
        ILogger<AccountLoginService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _characterService = characterService;
        _logger = logger;
    }

    /// <summary>
    /// Autentica um usuário com username e senha.
    /// </summary>
    public async Task<AccountLoginResult> AuthenticateAsync(
        string username, 
        string password, 
        CancellationToken cancellationToken = default)
    {
        // Validação de entrada
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return AccountLoginResult.Failure("Usuário e senha são obrigatórios.");
        }

        // Buscar conta (sem incluir relacionamentos - será feito pelo CharacterService)
        var account = await _dbContext.Accounts
            .AsTracking()
            .FirstOrDefaultAsync(a => a.Username == username, cancellationToken);

        if (account is null)
        {
            _logger.LogWarning("Login attempt failed: Account not found for username {Username}", username);
            return AccountLoginResult.Failure("Credenciais inválidas.");
        }

        // Validar estado da conta
        var validationResult = ValidateAccountState(account);
        if (!validationResult.IsValid)
        {
            _logger.LogWarning(
                "Login attempt failed for {Username}: {Reason}", 
                username, 
                validationResult.ErrorMessage);
            
            return AccountLoginResult.Failure(validationResult.ErrorMessage!);
        }

        // Verificar senha
        if (!_passwordHasher.VerifyPassword(account.PasswordHash, password))
        {
            _logger.LogWarning("Login attempt failed: Invalid password for username {Username}", username);
            return AccountLoginResult.Failure("Credenciais inválidas.");
        }

        // Buscar personagens usando o CharacterService
        var charactersResult = await _characterService.GetAccountCharactersAsync(
            account.Id, 
            cancellationToken);

        if (!charactersResult.Success)
        {
            _logger.LogError(
                "Failed to retrieve characters for account {AccountId}: {Message}", 
                account.Id, 
                charactersResult.Message);
            
            return AccountLoginResult.Failure("Erro ao carregar personagens.");
        }

        // Atualizar último login
        account.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Login successful for {Username} (Account ID: {AccountId}, Characters: {CharacterCount})",
            account.Username,
            account.Id,
            charactersResult.Characters.Length);
        
        account.Characters.Clear();
        foreach (var character in charactersResult.Characters)
            account.Characters.Add(character);

        return AccountLoginResult.From(account, charactersResult.Characters);
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

/// <summary>
/// Resultado da autenticação de conta.
/// </summary>
public sealed record AccountLoginResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public Account? Account { get; init; }
    public Character[] Characters { get; init; } = [];

    public static AccountLoginResult Failure(string message) => new()
    {
        Success = false,
        Message = message
    };

    public static AccountLoginResult From(Account account, Character[] characters) => new()
    {
        Success = true,
        Account = account,
        Characters = characters
    };
}