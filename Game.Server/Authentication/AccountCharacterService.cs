using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Persistence.Interfaces;

namespace Game.Server.Authentication;

public sealed class AccountCharacterService(IUnitOfWork unitOfWork, ILogger<AccountCharacterService> logger)
{
    // ========== CONSTANTS ==========
    
    private const int MaxCharactersPerAccount = 5;
    
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

        // ✅ Usar repositório especializado
        var characterCount = await unitOfWork.Characters.CountByAccountIdAsync(
            characterInfo.AccountId, cancellationToken);

        if (characterCount >= MaxCharactersPerAccount)
            return CharacterCreationResult.Failure($"Limite de {MaxCharactersPerAccount} personagens por conta atingido.");

        // ✅ Usar transação do Unit of Work
        await unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            // ✅ Verificar nome duplicado
            if (await unitOfWork.Characters.ExistsByNameAsync(characterInfo.Name, cancellationToken))
                return CharacterCreationResult.Failure("Nome do personagem já está em uso.");

            try
            {
                // Criar personagem
                var character = new Character(accountId: characterInfo.AccountId, name: characterInfo.Name,
                    gender: characterInfo.Gender, vocation: characterInfo.Vocation);
                
                await unitOfWork.Characters.AddAsync(character, cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken);
                
                logger.LogInformation("New character created: {CharacterName} (ID: {CharacterId})",
                    character.Name, character.Id);
                return CharacterCreationResult.From(character);
            }
            catch (ArgumentException e)
            {
                logger.LogError(e, "Validation error creating character: {CharacterName}", characterInfo.Name);
                return CharacterCreationResult.Failure(e.Message);
            }
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
}