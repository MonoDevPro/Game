using System;
using System.Linq;
using Game.Core.Security;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Game.Server.Authentication;

public sealed class AccountCharacterService(GameDbContext dbContext, ILogger<AccountCharacterService> logger)
{
    public record CharacterInfo(string Name, int Level, VocationType Vocation, Gender Gender);
    
    public record CharacterListResult(bool Success, string Message, Character[] Characters)
    {
        public static CharacterListResult Failure(string message) => new CharacterListResult(false, message, Array.Empty<Character>());
        public static CharacterListResult From(Character[] characters) => new CharacterListResult(true, string.Empty, characters);
    }
    public record CharacterCreationResult(bool Success, string Message, Character? Character)
    {
        public static CharacterCreationResult Failure(string message) => new CharacterCreationResult(false, message, null);
        public static CharacterCreationResult From(Character character) => new CharacterCreationResult(true, string.Empty, character);
    }
    
    public async Task<CharacterCreationResult> CreateCharacterAsync(CharacterInfo characterInfo, CancellationToken cancellationToken)
    {
        var nameTrimmed = characterInfo.Name.Trim();
        characterInfo = characterInfo with { Name = nameTrimmed };

        if (string.IsNullOrWhiteSpace(characterInfo.Name))
        {
            return CharacterCreationResult.Failure("Nome do personagem é obrigatório.");
        }

        if (await dbContext.Characters.AnyAsync(c => c.Name == characterInfo.Name, cancellationToken))
        {
            return CharacterCreationResult.Failure("Nome do personagem já está em uso.");
        }

        var character = new Character
        {
            Name = characterInfo.Name,
            Gender = characterInfo.Gender,
            Vocation = characterInfo.Vocation,
            Stats = new Stats()
            {
                Level = characterInfo.Level,
                Experience = 0,
            },
            Inventory = new Inventory(),
            PositionX = 5, // Posição inicial padrão
            PositionY = 5, // Posição inicial padrão
            DirectionEnum = DirectionEnum.South
        };
        await dbContext.Characters.AddAsync(character, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        logger.LogInformation("New character created: {CharacterName}", characterInfo.Name);
        return CharacterCreationResult.From(character);
    }
    
}
