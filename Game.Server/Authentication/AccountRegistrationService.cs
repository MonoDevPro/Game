using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Game.Core.Security;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Game.Server.Authentication;

public sealed class AccountRegistrationService
{
    private readonly GameDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AccountRegistrationService> _logger;

    public AccountRegistrationService(GameDbContext dbContext, IPasswordHasher passwordHasher, ILogger<AccountRegistrationService> logger)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task<AccountRegistrationResult> RegisterAsync(
        string username,
        string email,
        string password,
        string characterName,
        Gender gender,
        VocationType vocation,
        CancellationToken cancellationToken)
    {
        username = username.Trim();
        email = email.Trim();
        characterName = characterName.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(email))
        {
            return AccountRegistrationResult.Failure("Usuário, senha e e-mail são obrigatórios.");
        }

        if (string.IsNullOrWhiteSpace(characterName))
        {
            return AccountRegistrationResult.Failure("Nome do personagem é obrigatório.");
        }

        if (await _dbContext.Accounts.AnyAsync(a => a.Username == username, cancellationToken))
        {
            return AccountRegistrationResult.Failure("Nome de usuário já está em uso.");
        }

        if (await _dbContext.Accounts.AnyAsync(a => a.Email == email, cancellationToken))
        {
            return AccountRegistrationResult.Failure("E-mail já está cadastrado.");
        }

        if (await _dbContext.Characters.AnyAsync(c => c.Name == characterName, cancellationToken))
        {
            return AccountRegistrationResult.Failure("Nome de personagem já está em uso.");
        }

        try
        {
            var salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            var passwordHash = _passwordHasher.HashPassword(password);

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

            const int spawnX = 5;
            const int spawnY = 5;

            var character = new Character
            {
                Name = characterName,
                Gender = gender,
                Vocation = vocation,
                PositionX = spawnX,
                PositionY = spawnY,
                DirectionEnum = DirectionEnum.South,
                Account = account,
                Inventory = new Inventory
                {
                    Capacity = 30,
                    Slots = new List<InventorySlot>()
                },
                Stats = new Stats
                {
                    CurrentHp = 50,
                    CurrentMp = 30
                }
            };

            account.Characters.Add(character);

            await _dbContext.Accounts.AddAsync(account, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("New account registered: {Username} with character {Character}", username, characterName);

            return AccountRegistrationResult.Succeeded(account, character);
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database failure while registering account {Username}", username);
            return AccountRegistrationResult.Failure("Erro ao salvar dados no banco.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while registering account {Username}", username);
            return AccountRegistrationResult.Failure("Erro interno ao registrar conta.");
        }
    }
}

public sealed record AccountRegistrationResult
{
    public bool IsSuccess { get; init; }
    public string Message { get; init; } = string.Empty;
    public Account? Account { get; init; }
    public Character? Character { get; init; }

    public static AccountRegistrationResult Failure(string message) => new()
    {
        IsSuccess = false,
        Message = message
    };

    public static AccountRegistrationResult Succeeded(Account account, Character character) => new()
    {
        IsSuccess = true,
        Account = account,
        Character = character
    };
}
