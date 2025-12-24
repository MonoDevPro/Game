using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Game.Domain.Player;
using Game.Persistence.Interfaces;
using Game.Server.Security;

namespace Game.Server.Authentication;

public sealed record AccountRegistrationResult(bool IsSuccess, string Message, Account? Account)
{
    public static AccountRegistrationResult Failure(string message) => new(IsSuccess: false, Message: message, Account: null);
    public static AccountRegistrationResult Succeeded(Account account) => new(IsSuccess: true, Message: "Conta registrada com sucesso.", Account: account);
}

/// <summary>
/// Serviço responsável pelo registro de novas contas.
/// Implementa validações robustas, segurança e prevenção de duplicatas.
/// 
/// Autor: MonoDevPro
/// Data: 2025-10-12 21:35:22
/// </summary>
public sealed class AccountRegistrationService(
    IUnitOfWork unitOfWork,
    IPasswordHasher passwordHasher,
    ILogger<AccountRegistrationService> logger)
{
    // ========== CONSTANTS ==========
    
    private const int MinUsernameLength = 3;
    private const int MaxUsernameLength = 20;
    private const int MinPasswordLength = 8;
    private const int MaxPasswordLength = 128;
    private const int PasswordSaltSize = 32; // 256 bits
    
    // Regex para validação de email (RFC 5322 simplificado)
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);
    
    // Regex para validação de username (alfanumérico + underscore)
    private static readonly Regex UsernameRegex = new(
        @"^[a-zA-Z0-9_]+$",
        RegexOptions.Compiled);
    
    // Lista de usernames proibidos/reservados
    private static readonly HashSet<string> ReservedUsernames = new(StringComparer.OrdinalIgnoreCase)
    {
        "admin", "administrator", "moderator", "mod", "gm", "gamemaster",
        "support", "system", "server", "bot", "official", "staff",
        "root", "superuser", "null", "undefined", "guest"
    };
    
    // ========== FIELDS ==========

    // ========== CONSTRUCTOR ==========

    // ========== PUBLIC METHODS ==========
    
    /// <summary>
    /// Registra uma nova conta no sistema.
    /// </summary>
    public async Task<AccountRegistrationResult> RegisterAsync(
        string username,
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        // Normalizar e validar entrada...
        username = username?.Trim().ToLowerInvariant() ?? string.Empty;
        email = email?.Trim().ToLowerInvariant() ?? string.Empty;

        // Validações básicas...
        var usernameValidation = ValidateUsername(username);
        if (!usernameValidation.IsValid)
            return AccountRegistrationResult.Failure(usernameValidation.ErrorMessage!);

        var emailValidation = ValidateEmail(email);
        if (!emailValidation.IsValid)
            return AccountRegistrationResult.Failure(emailValidation.ErrorMessage!);

        var passwordValidation = ValidatePassword(password);
        if (!passwordValidation.IsValid)
            return AccountRegistrationResult.Failure(passwordValidation.ErrorMessage!);

        // ✅ Usar Unit of Work com transação
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // ✅ Usar repositório especializado
            if (await unitOfWork.Accounts.ExistsByUsernameAsync(username, cancellationToken))
            {
                logger.LogWarning("Registration failed: Username {Username} already exists", username);
                return AccountRegistrationResult.Failure("Nome de usuário já está em uso.");
            }

            if (await unitOfWork.Accounts.ExistsByEmailAsync(email, cancellationToken))
            {
                logger.LogWarning("Registration failed: Email {Email} already registered", MaskEmail(email));
                return AccountRegistrationResult.Failure("E-mail já está cadastrado.");
            }

            // Criar conta
            var salt = GenerateSalt();
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
                IsBanned = false,
                Characters = new List<Character>()
            };

            // ✅ Adicionar via repositório
            await unitOfWork.Accounts.AddAsync(account, cancellationToken);
            
            // ✅ Commit da transação (já faz SaveChanges internamente)
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation(
                "New account registered: {Username} (ID: {AccountId})",
                username,
                account.Id);

            return AccountRegistrationResult.Succeeded(account);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error registering account {Username}", username);
            return AccountRegistrationResult.Failure("Erro interno ao registrar conta.");
        }
    }
    
    /// <summary>
    /// Verifica se um username está disponível.
    /// </summary>
    public async Task<bool> IsUsernameAvailableAsync(
        string username, 
        CancellationToken cancellationToken = default)
    {
        username = username.Trim().ToLowerInvariant() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(username))
            return false;
        
        var validation = ValidateUsername(username);
        if (!validation.IsValid)
            return false;
        
        return !await unitOfWork.Accounts
            .ExistsByUsernameAsync(username, cancellationToken);
    }
    
    /// <summary>
    /// Verifica se um email está disponível.
    /// </summary>
    public async Task<bool> IsEmailAvailableAsync(
        string email, 
        CancellationToken cancellationToken = default)
    {
        email = email.Trim().ToLowerInvariant() ?? string.Empty;
        
        if (string.IsNullOrWhiteSpace(email))
            return false;
        
        var validation = ValidateEmail(email);
        if (!validation.IsValid)
            return false;
        
        return !await unitOfWork.Accounts
            .ExistsByEmailAsync(email, cancellationToken);
    }
    
    // ========== VALIDATION METHODS ==========
    
    private static (bool IsValid, string? ErrorMessage) ValidateUsername(string username)
    {
        // Tamanho
        if (username.Length < MinUsernameLength)
        {
            return (false, $"Nome de usuário deve ter no mínimo {MinUsernameLength} caracteres.");
        }
        
        if (username.Length > MaxUsernameLength)
        {
            return (false, $"Nome de usuário deve ter no máximo {MaxUsernameLength} caracteres.");
        }
        
        // Formato (apenas alfanumérico e underscore)
        if (!UsernameRegex.IsMatch(username))
        {
            return (false, "Nome de usuário pode conter apenas letras, números e underscore (_).");
        }
        
        // Não pode começar com número ou underscore
        if (char.IsDigit(username[0]) || username[0] == '_')
        {
            return (false, "Nome de usuário não pode começar com número ou underscore.");
        }
        
        // Não pode terminar com underscore
        if (username[^1] == '_')
        {
            return (false, "Nome de usuário não pode terminar com underscore.");
        }
        
        // Não pode ter underscores consecutivos
        if (username.Contains("__"))
        {
            return (false, "Nome de usuário não pode conter underscores consecutivos.");
        }
        
        // Verificar usernames reservados
        if (ReservedUsernames.Contains(username))
        {
            return (false, "Este nome de usuário está reservado.");
        }
        
        // Verificar palavras ofensivas (você pode expandir esta lista)
        if (ContainsOffensiveWords(username))
        {
            return (false, "Nome de usuário contém palavras não permitidas.");
        }
        
        return (true, null);
    }
    
    private static (bool IsValid, string? ErrorMessage) ValidateEmail(string email)
    {
        // Formato básico
        if (!EmailRegex.IsMatch(email))
        {
            return (false, "Formato de e-mail inválido.");
        }
        
        // Tamanho máximo
        if (email.Length > 254) // RFC 5321
        {
            return (false, "E-mail muito longo.");
        }
        
        // Verificar domínios descartáveis (você pode expandir esta lista)
        var disposableDomains = new[] 
        { 
            "tempmail.com", "throwaway.email", "guerrillamail.com",
            "10minutemail.com", "mailinator.com"
        };
        
        var domain = email.Split('@')[1];
        if (disposableDomains.Contains(domain, StringComparer.OrdinalIgnoreCase))
        {
            return (false, "E-mails temporários não são permitidos.");
        }
        
        return (true, null);
    }
    
    private static (bool IsValid, string? ErrorMessage) ValidatePassword(string password)
    {
        // Tamanho
        if (password.Length < MinPasswordLength)
        {
            return (false, $"Senha deve ter no mínimo {MinPasswordLength} caracteres.");
        }
        
        if (password.Length > MaxPasswordLength)
        {
            return (false, $"Senha muito longa (máximo {MaxPasswordLength} caracteres).");
        }
        
        // Complexidade: pelo menos uma letra maiúscula
        if (!password.Any(char.IsUpper))
        {
            return (false, "Senha deve conter pelo menos uma letra maiúscula.");
        }
        
        // Complexidade: pelo menos uma letra minúscula
        if (!password.Any(char.IsLower))
        {
            return (false, "Senha deve conter pelo menos uma letra minúscula.");
        }
        
        // Complexidade: pelo menos um número
        if (!password.Any(char.IsDigit))
        {
            return (false, "Senha deve conter pelo menos um número.");
        }
        
        // Complexidade: pelo menos um caractere especial
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            return (false, "Senha deve conter pelo menos um caractere especial.");
        }
        
        // Verificar senhas comuns
        var commonPasswords = new[] 
        { 
            "password", "123456", "12345678", "qwerty", "abc123",
            "password123", "admin123", "welcome123"
        };
        
        if (commonPasswords.Contains(password, StringComparer.OrdinalIgnoreCase))
        {
            return (false, "Senha muito comum. Escolha uma senha mais segura.");
        }
        
        return (true, null);
    }
    
    // ========== UTILITY METHODS ==========
    
    private static byte[] GenerateSalt()
    {
        var salt = new byte[PasswordSaltSize];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }
    
    private static bool ContainsOffensiveWords(string text)
    {
        // Lista básica de palavras ofensivas (expanda conforme necessário)
        var offensiveWords = new[] 
        { 
            "admin", "moderator", "fuck", "shit", "bitch" 
            // Adicione mais conforme necessário
        };
        
        return offensiveWords.Any(word => 
            text.Contains(word, StringComparison.OrdinalIgnoreCase));
    }
    
    private static string MaskEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return string.Empty;
        
        var parts = email.Split('@');
        if (parts.Length != 2)
            return email;
        
        var localPart = parts[0];
        var domain = parts[1];
        
        if (localPart.Length <= 2)
            return $"{localPart[0]}***@{domain}";
        
        return $"{localPart[0]}***{localPart[^1]}@{domain}";
    }
}