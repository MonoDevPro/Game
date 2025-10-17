using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Persistence.Interfaces;

namespace Game.Server.Authentication;

public sealed class AccountCharacterService(IUnitOfWork unitOfWork, ILogger<AccountCharacterService> logger)
{
    // ========== CONSTANTS ==========
    
    private const int MinCharacterNameLength = 3;
    private const int MaxCharacterNameLength = 20;
    private const int MaxCharactersPerAccount = 5;
    private const int DefaultInventoryCapacity = 30;
    
    // Posição inicial padrão
    private const int DefaultSpawnX = 5;
    private const int DefaultSpawnY = 5;
    private const int DefaultSpawnZ = 0;
    private const int DefaultFacingX = 0;
    private const int DefaultFacingY = 1;
    
    // ========== RECORDS ==========
    
    public record CharacterInfo(
        int AccountId, 
        string Name, 
        int Level, 
        VocationType Vocation, 
        Gender Gender);
    
    public record CharacterListResult(bool Success, string Message, Character[] Characters)
    {
        public static CharacterListResult Failure(string message) => 
            new(false, message, Array.Empty<Character>());
        
        public static CharacterListResult From(Character[] characters) => 
            new(true, string.Empty, characters);
    }
    
    public record CharacterCreationResult(bool Success, string Message, Character? Character)
    {
        public static CharacterCreationResult Failure(string message) => 
            new(false, message, null);
        
        public static CharacterCreationResult From(Character character) => 
            new(true, string.Empty, character);
    }
    
    public record CharacterDeletionResult(bool Success, string Message)
    {
        public static CharacterDeletionResult Failure(string message) => 
            new(false, message);
        
        public static CharacterDeletionResult Ok() => 
            new(true, string.Empty);
    }
    
    // ========== PUBLIC METHODS ==========
    
    /// <summary>
    /// Lista todos os personagens de uma conta.
    /// </summary>
    public async Task<CharacterListResult> GetAccountCharactersAsync(
        int accountId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ Usar repositório especializado
            var characters = await unitOfWork.Characters.GetByAccountIdAsync(accountId, cancellationToken);

            logger.LogInformation("Retrieved {Count} characters for account {AccountId}", 
                characters.Length, accountId);

            return CharacterListResult.From(characters);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving characters for account {AccountId}", accountId);
            return CharacterListResult.Failure("Erro ao buscar personagens.");
        }
    }

    public async Task<CharacterCreationResult> CreateCharacterAsync(
        CharacterInfo characterInfo, 
        CancellationToken cancellationToken = default)
    {
        var nameTrimmed = characterInfo.Name.Trim();
        characterInfo = characterInfo with { Name = nameTrimmed };

        var validationResult = ValidateCharacterName(characterInfo.Name);
        if (!validationResult.IsValid)
            return CharacterCreationResult.Failure(validationResult.ErrorMessage!);

        // ✅ Usar repositório especializado
        var characterCount = await unitOfWork.Characters.CountByAccountIdAsync(
            characterInfo.AccountId, cancellationToken);

        if (characterCount >= MaxCharactersPerAccount)
        {
            return CharacterCreationResult.Failure(
                $"Limite de {MaxCharactersPerAccount} personagens por conta atingido.");
        }

        // ✅ Usar transação do Unit of Work
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // ✅ Verificar nome duplicado
            if (await unitOfWork.Characters.ExistsByNameAsync(characterInfo.Name, cancellationToken))
            {
                return CharacterCreationResult.Failure("Nome do personagem já está em uso.");
            }

            // Criar personagem
            var character = new Character
            {
                AccountId = characterInfo.AccountId,
                Name = characterInfo.Name,
                Gender = characterInfo.Gender,
                Vocation = characterInfo.Vocation,
                PositionX = DefaultSpawnX,
                PositionY = DefaultSpawnY,
                PositionZ = DefaultSpawnZ,
                FacingX = DefaultFacingX,
                FacingY = DefaultFacingY
            };

            character.Stats = CreateInitialStats(character, characterInfo.Vocation);
            character.Inventory = CreateInitialInventory(character);

            // ✅ Adicionar via repositório
            await unitOfWork.Characters.AddAsync(character, cancellationToken);
            await unitOfWork.CommitTransactionAsync(cancellationToken);

            logger.LogInformation("New character created: {CharacterName} (ID: {CharacterId})",
                character.Name, character.Id);

            return CharacterCreationResult.From(character);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating character: {CharacterName}", characterInfo.Name);
            return CharacterCreationResult.Failure("Erro ao criar personagem.");
        }
    }
    
    /// <summary>
    /// Deleta um personagem de uma conta (com validação de propriedade).
    /// </summary>
    public async Task<CharacterDeletionResult> DeleteCharacterAsync(
        int accountId,
        int characterId, 
        CancellationToken cancellationToken = default)
    {
        try
        {
            // ✅ Usar método especializado do repositório
            var character = await unitOfWork.Characters
                .GetByIdWithRelationsForDeletionAsync(characterId, cancellationToken);
        
            if (character is null)
            {
                return CharacterDeletionResult.Failure("Personagem não encontrado.");
            }
        
            // ✅ Validar propriedade
            if (character.AccountId != accountId)
            {
                logger.LogWarning(
                    "Account {AccountId} attempted to delete character {CharacterId} owned by {OwnerId}",
                    accountId,
                    characterId,
                    character.AccountId);
            
                return CharacterDeletionResult.Failure("Você não pode deletar este personagem.");
            }
        
            // ✅ Deletar via repositório (usa o método DeleteAsync do Repository<T>)
            await unitOfWork.Characters.DeleteAsync(characterId, cancellationToken);
        
            // ✅ Salvar mudanças via Unit of Work
            await unitOfWork.SaveChangesAsync(cancellationToken);
        
            logger.LogInformation(
                "Character deleted: {CharacterName} (ID: {CharacterId}, Account: {AccountId})",
                character.Name,
                characterId,
                accountId);
        
            return CharacterDeletionResult.Ok();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting character {CharacterId}", characterId);
            return CharacterDeletionResult.Failure("Erro ao deletar personagem.");
        }
    }
    
    // ========== PRIVATE METHODS ==========
    
    private static (bool IsValid, string? ErrorMessage) ValidateCharacterName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return (false, "Nome do personagem é obrigatório.");
        }
        
        if (name.Length < MinCharacterNameLength)
        {
            return (false, $"Nome deve ter no mínimo {MinCharacterNameLength} caracteres.");
        }
        
        if (name.Length > MaxCharacterNameLength)
        {
            return (false, $"Nome deve ter no máximo {MaxCharacterNameLength} caracteres.");
        }
        
        // Validar caracteres permitidos (apenas letras, sem números ou símbolos)
        if (!name.All(c => char.IsLetter(c) || c == ' '))
        {
            return (false, "Nome pode conter apenas letras e espaços.");
        }
        
        // Validar que não começa/termina com espaço
        if (name.StartsWith(' ') || name.EndsWith(' '))
        {
            return (false, "Nome não pode começar ou terminar com espaço.");
        }
        
        // Validar espaços consecutivos
        if (name.Contains("  "))
        {
            return (false, "Nome não pode conter espaços consecutivos.");
        }
        
        return (true, null);
    }
    
    private static Stats CreateInitialStats(Character character, VocationType vocation)
    {
        // Stats base variam por vocação
        var (strength, dexterity, intelligence, constitution, spirit) = vocation switch
        {
            VocationType.Warrior => (15, 10, 5, 15, 5),
            VocationType.Mage => (5, 8, 18, 8, 11),
            VocationType.Archer => (8, 16, 6, 10, 10),
            _ => (10, 10, 10, 10, 10) // Novice/Default
        };
        
        var maxHp = CalculateMaxHp(constitution, 1);
        var maxMp = CalculateMaxMp(spirit, intelligence, 1);
        
        return new Stats
        {
            CharacterId = character.Id,
            Character = character,
            Level = 1,
            Experience = 0,
            BaseStrength = strength,
            BaseDexterity = dexterity,
            BaseIntelligence = intelligence,
            BaseConstitution = constitution,
            BaseSpirit = spirit,
            CurrentHp = maxHp,
            CurrentMp = maxMp
        };
    }
    
    private static Inventory CreateInitialInventory(Character character)
    {
        return new Inventory
        {
            CharacterId = character.Id,
            Character = character,
            Capacity = DefaultInventoryCapacity
        };
    }
    
    private static int CalculateMaxHp(int constitution, int level)
    {
        // HP = (Constitution * 10) + (Level * 5)
        return (constitution * 10) + (level * 5);
    }
    
    private static int CalculateMaxMp(int spirit, int intelligence, int level)
    {
        // MP = ((Spirit + Intelligence) * 5) + (Level * 3)
        return ((spirit + intelligence) * 5) + (level * 3);
    }
}